namespace Agience.Client
{
    public class Runner
    {
        private readonly Agent _agent;
        private readonly string _informationId;

        private Runner() { }

        internal Runner(Agent agent, string informationId)
        {
            _agent = agent;
            _informationId = informationId;
        }     

        public async Task<Data?> Dispatch<T>(Data? input = null, OutputCallback? outputCallback = null) where T : Template, new()
        {

            return await _agent.Dispatch<T>(input, outputCallback);
        }
        public async Task<Data?> Dispatch(string templateId, Data? input = null, OutputCallback? outputCallback = null)
        {

            return await _agent.Dispatch(templateId, input, outputCallback);
        }
        public async Task Prompt(Data? input, OutputCallback outputCallback)
        {

            return await _agent.Prompt(input, outputCallback);
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
