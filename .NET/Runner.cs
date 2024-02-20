using Agience.Model;
using System.Text;

namespace Agience.Client
{
    public class DispatchResponse
    {
        public Runner Runner { get; set; }
        public Data? Output { get; set; }

        public DispatchResponse(Runner runner, Data? output = null)
        {
            Runner = runner;
            Output = output;
        }
    }

    public class Runner
    {
        public string? AgentId => _agent.Id;
        public string? AgencyId => _agent.Agency.Id;

        private readonly Agent _agent;
        private Information? _information;

        private Runner() { throw new NotImplementedException(); }

        public Runner(Agent agent, Information? information = null)
        {
            _agent = agent;
            _information = information;
        }

        public async Task<DispatchResponse> Dispatch<T>(Data? input = null, OutputCallback? localCallback = null) where T : Template, new()
        {
            // TODO: Allow registering a Type with a different templateIds, so we can have multiple templates with the same handler.
            var templateId = typeof(T).FullName;

            if (!string.IsNullOrEmpty(templateId))
            {
                return await Dispatch(templateId, input, localCallback);
            }

            return new(this, null);
        }

        public async Task<DispatchResponse> Dispatch(string templateId, Data? input = null, OutputCallback? localCallback = null)
        {
            if (_information == null)
            {
                _information = new Information()
                {
                    Input = input,
                    InputAgentId = _agent.Id,
                    InputTimestamp = _agent.Timestamp,
                    TemplateId = templateId
                };

                return await Dispatch(localCallback);
            }
            else
            {
                var information = new Information()
                {
                    Input = input,
                    InputAgentId = _agent.Id,
                    InputTimestamp = _agent.Timestamp,
                    TemplateId = templateId,
                    ParentInformationId = _information.Id
                };

                return await new Runner(_agent, information).Dispatch(localCallback);
            }
        }

        public async Task<DispatchResponse> Dispatch(OutputCallback? localCallback = null)
        {

            var result = new DispatchResponse(this, null);

            if (_information?.TemplateId != null)
            {
                if (_agent.Templates.TryGetValue(_information.TemplateId, out (Template, OutputCallback?) templateAndGlobalCallback))
                {
                    var (agentTemplate, globalCallback) = templateAndGlobalCallback;

                    _information.Transformation = agentTemplate.Description;

                    result = await Dispatch(agentTemplate, localCallback, globalCallback);
                }

                else if (_agent.Agency.Templates.TryGetValue(_information.TemplateId, out Model.Template? agencyTemplate) && agencyTemplate.AgentId != null)
                {
                    result = await Dispatch(agencyTemplate, localCallback);
                }
            }
            // TODO: Invoke Event Notificaiton

            return result;
        }

        private async Task<DispatchResponse> Dispatch(Template template, OutputCallback? localCallback, OutputCallback? globalCallback)
        {
            // Process this locally

            if (_information != null)
            {
                // TODO: Debounce Assessments
                if (await template.Assess(this, _information.Input))
                {
                    var output = await template.Process(this, _information.Input);

                    _information.Output = output;
                    _information.OutputAgentId = template.Agent?.Id;
                    _information.OutputTimestamp = _agent.Timestamp;
                }

                _agent.History.Add(_information);

                await Task.WhenAll(
                    localCallback?.Invoke(this, _information.Output) ?? Task.CompletedTask,
                    globalCallback?.Invoke(this, _information.Output) ?? Task.CompletedTask
                ).ConfigureAwait(false);

                return new (this, _information.Output);
            }

            return new (this, null);
        }

        internal void ReceiveOutput(Information information)
        {
            if (_information == null)
            {
                throw new InvalidOperationException("No Information to receive output.");
            }

            if (string.IsNullOrEmpty(information?.OutputTimestamp) || string.IsNullOrEmpty(information?.OutputAgentId))
            {
                throw new InvalidOperationException("Incoming Information is incomplete.");
            }

            _information.Output = information.Output;
            _information.OutputAgentId = information.OutputAgentId;
            _information.OutputTimestamp = information.OutputTimestamp;
        }

        private async Task<DispatchResponse> Dispatch(Model.Template template, OutputCallback? localCallback)
        {
            // Process this remotely

            if (_information != null && template?.AgentId != null)
            {
                await _agent.SendInformationToAgent(_information, template.AgentId, this);

                while (string.IsNullOrEmpty(_information.OutputTimestamp))
                {
                    Task.Delay(10).Wait();
                }

                if (localCallback != null)
                {
                    await localCallback.Invoke(this, _information.Output);
                }

                return new(this, _information.Output);
            }

            return new(this, null);
        }

        public async Task<DispatchResponse> Prompt(Data? input = null)
        {
            return await Dispatch(_agent.Agency.DefaultTemplates["prompt"], input);
        }

        public async Task<DispatchResponse> Context(Data? input = null)
        {
            return await Dispatch(_agent.Agency.DefaultTemplates["context"], input);
        }

        public async Task<DispatchResponse> Echo(Data? input = null)
        {
            return await Dispatch(_agent.Agency.DefaultTemplates["echo"], input);
        }

        public async Task<DispatchResponse> Debug(Data? input = null)
        {
            return await Dispatch(_agent.Agency.DefaultTemplates["debug"], input);
        }

        public async Task<DispatchResponse> History(Data? input = null)
        {
            return await Dispatch(_agent.Agency.DefaultTemplates["history"], input);
        }

        public async Task<DispatchResponse> Log(Data? input = null)
        {
            return await Dispatch(_agent.Agency.DefaultTemplates["log"], input);
        }
    }
}