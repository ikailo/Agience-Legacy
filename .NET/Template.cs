using System.Security.Cryptography;
using System.Text;

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



    public class Template
    {  
        public string Id { get; private set; }                  
        public Data? Description { get; set; } // Description is the template identifier. Future: Searchable, Inferable, Unique, Persist.
        public string[]? InputKeys { get; set; }
        public string[]? OutputKeys { get; set; }
        public Agent? Agent { get; internal set; }
        public virtual Task<Data?> Process(Data? data) => Task.FromResult<Data?>(null);
        public Template()
        {   
            Id = GetType().FullName ?? throw new ArgumentNullException("Type.FullName");            
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