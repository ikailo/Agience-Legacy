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


    public class Template //: Model.Template
    {
        

        //public event Func<Agent, Data?, Task<Data?>>? OnCallback;
        public Agent Agent { get; internal set; }

        public Template(Agent agent)
        {
            Agent = agent;
        }

        public Template() { }

        private string? _id;
        // Template Ids are generated based on the template definition. 
        // TODO: Review, since this is not a unique identifier.
        public new string Id
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
        }

        // Description is the template identifier. Searchable, Inferable, Unique.
        public Data? Description { get; set; }
        public string[]? InputKeys { get; set; }
        public string[]? OutputKeys { get; set; }

        public virtual Task<Data?> Process(Data? data) => Task.FromResult<Data?>(null);
        /*
        public async Task<Data?> Callback(Data? data = null)
        {
            if (OnCallback != null)
            {
                return await OnCallback.Invoke(this.Agent, data);
            }

            return null;
        }*/
    }
}