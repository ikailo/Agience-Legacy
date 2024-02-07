namespace Agience.Client
{
    /*
       // https://github.com/daveshap/ACE_Framework/blob/main/publications/Conceptual%20Framework%20for%20Autonomous%20Cognitive%20Entities%20(ACE).pdf
       public enum LayerDefinition
       {
           ASPIRATION = 0,
           GLOBAL_STRATEGY = 1,
           AGENT_MODEL = 2,
           EXECUTIVE_FUNCTION = 3,
           COGNITIVE_CONTROL = 4,
           TASK_PROSECUTION = 5
       }*/

    public abstract class Template
    {
        public string Id { get; }
        public abstract Data? Description { get; } // Description is the template identifier. Future: Searchable, Inferable, Unique, Persist.
        public virtual string[]? InputKeys { get; protected set; }
        public virtual string[]? OutputKeys { get; protected set; }
        protected internal Agent Agent
        {
            get { return _agent ?? throw new ArgumentNullException(nameof(Agent)); }
            internal set { _agent = value; }
        }

        private Agent? _agent;

        public Template()
        {
            Id = GetType().FullName ?? throw new ArgumentNullException("Type.FullName");
        }

        internal Template(Agent? agent) : this()
        {
            _agent = agent;
        }

        protected internal virtual Task<bool> Assess(Data? input = null) => Task.FromResult(true);

        protected internal abstract Task<Data?> Process(Data? input = null);

        protected async Task<Data?> Dispatch<T>(Data? input = null) where T : Template, new()
        {
            return await Agent.Dispatch<T>(input);
        }

        protected async Task<Data?> Dispatch(string templateId, Data? input = null)
        {
            return await Agent.Dispatch(templateId, input);
        }

        protected async Task<Data?> Prompt(Data? input = null)
        {
            return await Agent.Prompt(input);
        }

        internal Model.Template ToAgienceModel()
        {
            return new Model.Template()
            {
                Id = Id,
                Description = Description,
                InputKeys = InputKeys,
                OutputKeys = OutputKeys,
                AgentId = Agent?.Id
            };
        }

        /*
        private string? _id;

        

        // Template Ids are generated based on the template definition. 
        // TODO: Review, since this is not a unique identifier.
        public string Id
        {
            get
            {
                if (_id != null) return _id;

                // Concatenate the strings to form the input
                // TODO: Cannonicalize the Id. Sort the arrays.
                string input = (Description?.Raw ?? "") +
                               string.Join("", InputKeys ?? Array.Empty<string>()) +
                               string.Join("", OutputKeys ?? Array.Empty<string>());

                // Use SHA256 to hash the input string
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    // Convert the input string to a byte array and compute the hash
                    byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                    // Convert the byte array to a Base64 URL-safe string
                    string base64UrlSafeString = Convert.ToBase64String(bytes)
                        .TrimEnd('=') // Remove any trailing '=' used for padding
                        .Replace('+', '-') // Replace '+' with '-'
                        .Replace('/', '_'); // Replace '/' with '_'

                    return base64UrlSafeString;
                }
            }
            protected set
            {
                _id = value;
            }
        }*/
    }
}