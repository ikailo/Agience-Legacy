using Agience;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;

namespace Agience.Client.MQTT.Model
{
    public enum TemplateState
    {
        RESTING = 0,
        ASSESSING = 1,
        PROCESSING = 2
    }

    public class Template : Agience.Model.Template
    {        
        public Agent? Agent { get; set; }

        public Template(Agent? agent)
        {
            Agent = agent;
        }

        private string? _id;

        // Template Ids are generated based on the template definition. Not guaranteed persistant.
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
        public Template() { }
        public virtual Task<bool> Assess(Information information) => Task.FromResult(false);
        public virtual Task<Data?> Process(Information information) => Task.FromResult((Data?)null);
    }
}