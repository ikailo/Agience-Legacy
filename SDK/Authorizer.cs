using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Web;
using AutoMapper;
using static System.Formats.Asn1.AsnWriter;

namespace Agience.SDK
{
    [AutoMap(typeof(Models.Entities.Authorizer), ReverseMap = true)]
    public class Authorizer : Models.Entities.Authorizer
    {
        
        public Authorizer() { }

        public string? GetAuthorizationUri(string authorityUri)
        {
            var state = HttpUtility.UrlEncode(""); // TODO: Build the State. Nonce, etc..

            if (AuthType == Models.Entities.AuthorizationType.None)
            {
                return null;
            }
            else if (AuthType == Models.Entities.AuthorizationType.OAuth2)
            {
                var clientId = HttpUtility.UrlEncode(ClientId);
                var redirectUri = HttpUtility.UrlEncode($"{authorityUri}{RedirectUri}");
                var scope = HttpUtility.UrlEncode(Scope);                

                return $"{AuthUri}?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={state}";
            }
            else if (AuthType == Models.Entities.AuthorizationType.ApiKey)
            {
                return HttpUtility.UrlEncode($"{authorityUri}/manage/authorizer/{Id}/authorize?state={state}");
            }

            throw new InvalidOperationException("Unknown authorization type");            
        }

        public async Task Activate(string code, string state)
        {   

            // Activate the authorizer
            throw new NotImplementedException();
        }
    }
}

