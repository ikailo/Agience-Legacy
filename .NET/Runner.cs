using Agience.Model;

namespace Agience.Client
{
    public class Runner : IDisposable
    {
        private readonly Agent _agent;
        private readonly string? _informationId;

        private Runner() { throw new NotImplementedException(); }

        internal Runner(Agent agent, string? informationId = null)
        {
            _agent = agent;
            _informationId = informationId;
        }     

        // For when the template type is known and local
        public async Task<Data?> Dispatch<T>(Data? input = null, OutputCallback? localCallback = null) where T : Template, new()
        {
            var templateId = typeof(T).FullName;

            if (string.IsNullOrEmpty(templateId) || !_agent.Templates.ContainsKey(templateId))
            {
                return null;
            }

            return await Dispatch(templateId, input, localCallback);
        }

        // For when the templateId is known. Local or remote.
        public async Task<Data?> Dispatch(string templateId, Data? input = null, OutputCallback? localCallback = null)
        {
            // Check if the template is local, if so Invoke it.
            if (_agent.Templates.TryGetValue(templateId, out (Template, OutputCallback?) templateAndCallback))
            {
                var (agentTemplate, globalCallback) = templateAndCallback;

                var information = new Information()
                {
                    Input = input,
                    InputAgentId = _agent.Id,
                    TemplateId = agentTemplate.Id,
                    Transformation = agentTemplate.Description
                };

                if (await agentTemplate.Assess(information))
                {

                    information = await agentTemplate.Process(information);

                    // TODO: Information Tracking. Keep track of hierarchy, which templates were invoked and from which , etc.

                    // Invoke any callbacks
                    await Task.WhenAll(
                        localCallback?.Invoke(_agent, information.Output) ?? Task.CompletedTask,
                        globalCallback?.Invoke(_agent, information.Output) ?? Task.CompletedTask
                    ).ConfigureAwait(false);

                    return information.Output;
                }

                return null;
            }

            // If not, try to find it in the Agency and dispatch it directly to the agent.
            if (_agent.Agency.Templates.TryGetValue(templateId, out Model.Template? agencyTemplate))
            {
                // Send this via broker to the agent



            }

            return null;
        }

        // For when the template is not known
        public async Task<Data?> Prompt(Data? input = null, OutputCallback? localCallback = null)
        {
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
