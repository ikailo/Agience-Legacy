using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using AutoMapper;

namespace Agience.SDK
{
    [AutoMap(typeof(Models.Entities.Authorizer), ReverseMap = true)]
    public class Authorizer : Models.Entities.Authorizer
    {
        public Authorizer() { }

        public async Task Activate()
        {
            // Activate the authorizer
        }

        public async Task Deactivate()
        {
            // Deactivate the authorizer
        }
    }
}

