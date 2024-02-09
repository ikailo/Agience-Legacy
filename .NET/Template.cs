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

        internal Agent? Agent;

        public Template()
        {
            Id = GetType().FullName ?? throw new ArgumentNullException("Type.FullName");
        }

        protected internal virtual Task<bool> Assess(Runner runner, Data? input = null) => Task.FromResult(true);
        protected internal abstract Task<Data?> Process(Runner runner, Data? input = null);     

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

        internal async Task<bool> Assess(Information information)
        {
            return await Assess(information.Input);
        }

        internal async Task<Information> Process(Information information)
        {
            var runner = new Runner(Agent!, information.Id);

            information.Output = await Process(runner, information.Input);
            information.OutputAgentId = Agent!.Id;
            return information;            
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