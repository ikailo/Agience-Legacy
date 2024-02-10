using Agience.Model;

namespace Agience.Client
{
    public class Runner : IDisposable
    {
        private readonly Agent _agent;
        private Information? _information;

        private Runner() { throw new NotImplementedException(); }

        public Runner(Agent agent, Information? information = null)
        {
            _agent = agent;
            _information = information;
        }

        public async Task<(Runner, Data?)> Dispatch<T>(Data? input = null, OutputCallback? localCallback = null) where T : Template, new()
        {
            var templateId = typeof(T).FullName;

            if (!string.IsNullOrEmpty(templateId))
            {
                return await Dispatch(templateId, input, localCallback);
            }

            return (this, null);
        }

        public async Task<(Runner, Data?)> Dispatch(string templateId, Data? input = null, OutputCallback? localCallback = null)
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

        public async Task<(Runner, Data?)> Dispatch(OutputCallback? localCallback = null)
        {
            if (_information?.TemplateId != null)
            {
                if (_agent.Templates.TryGetValue(_information.TemplateId, out (Template, OutputCallback?) templateAndGlobalCallback))
                {
                    var (agentTemplate, globalCallback) = templateAndGlobalCallback;

                    _information.Transformation = agentTemplate.Description;

                    return await Dispatch(agentTemplate, localCallback, globalCallback);
                }

                else if (_agent.Agency.Templates.TryGetValue(_information.TemplateId, out Model.Template? agencyTemplate) && agencyTemplate.AgentId != null)
                {
                    return await Dispatch(agencyTemplate, localCallback);
                }
            }
            return (this, null);
        }

        private async Task<(Runner, Data?)> Dispatch(Template template, OutputCallback? localCallback, OutputCallback? globalCallback)
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

                // TODO: Write to local timeline                

                await Task.WhenAll(
                    localCallback?.Invoke(this, _information.Output) ?? Task.CompletedTask,
                    globalCallback?.Invoke(this, _information.Output) ?? Task.CompletedTask
                ).ConfigureAwait(false);

                return (this, _information.Output);
            }

            return (this, null);
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

        private async Task<(Runner, Data?)> Dispatch(Model.Template template, OutputCallback? localCallback)
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

                return (this, _information.Output);
            }

            return (this, null);
        }

        public async Task<Data?> Prompt(Data? input = null, OutputCallback? localCallback = null)
        {
            // TODO: Get the default prompt template and dispatch it
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }




        //public async Task Retrieve() { }
        //public async Task Record() { }
        //public async Task Log() { }
        //public async Task Validate() { }
        //public async Task Sanitize() { }
        //public async Task Monitor() { }
        //public async Task Predict() { }
        //public async Task Transcribe() { }
    }
}
