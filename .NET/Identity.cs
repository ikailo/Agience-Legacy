using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Security.Claims;
using Agience.Client.Model;

namespace Agience.Client
{
    public class Identity
    {
        public string? Name { get; private set; }
        public Authority Authority { get; private set; }
        public string? InstanceId { get; private set; }
        public string? AgencyId { get; private set; }
        public string? AgentId { get; private set; }               

        internal Dictionary<string, string?> Tokens = new();

        //private string? _clientId;
        private string? _clientSecret;

        /*
        internal string PublishAgentMask => $"{AgencyId}/+";
        internal string SubscribeAgentMask => $"{AgencyId}/{AgentId}";
        internal string PublishAgencyMask => $"{AgencyId}/+";
        internal string SubscribeAgencyMask => $"{AgencyId}/0";
        */

        //internal string Address => $"{InstanceId ?? "-"}/{AgencyId ?? "-"}/{AgentId ?? "-"}";
        /*
        public Identity(string authorityUri, string clientId, string clientSecret)
        {
            Authority = new Authority(authorityUri);
            InstanceId = clientId;
            _clientSecret = clientSecret;

            //RefreshAccessToken().Wait(); // TODO: On Demand
        }*/
        /*
        private async Task RefreshAccessToken()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Base64UrlEncoder.Encode($"{InstanceId}:{_clientSecret}"));
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var parameters = new Dictionary<string, string>();
                parameters.Add("grant_type", "client_credentials");
                parameters.Add("scope", $"connect");

                var endpoint = $"{Authority?.AuthorityUri}/token";

                var httpResponse = await httpClient.PostAsJsonAsync(endpoint, parameters);

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var tokenResponse = await httpResponse.Content.ReadFromJsonAsync<TokenResponse>();

                    if (tokenResponse?.token_type != null)
                    {
                        var claims = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse.access_token).Claims;

                        foreach (Claim claim in claims)
                        {
                            if (claim.Type == "name")
                            {
                                Name = claim.Value;
                            }                                                    
                            if (claim.Type == "agency_id")
                            {
                                AgencyId = claim.Value;
                            }
                            if (claim.Type == "agent_id")
                            {
                                AgentId = claim.Value;
                            }
                            if (claim.Type == "instance_id")
                            {
                                InstanceId = claim.Value;
                            }
                            if (claim.Type == "aud")
                            {
                                Tokens[claim.Value] = tokenResponse.access_token;
                            }
                        }
                        return;
                    }
                }
            }
        }*/
        /*
        public string GetMaskedTopic(string topic)
        {
            string[] topicParts = topic.Split('/');
            string[] maskParts = PublishMask.Split('/');

            if (topicParts.Length != 2)
            {
                throw new ArgumentException(nameof(topic));
            }

            for (int i = 0; i < topicParts.Length; i++)
            {
                topicParts[i] = maskParts[i] == "+" ? topicParts[i] : maskParts[i];
            }
            return string.Join('/', topicParts);
        }
        */
        

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }
    }
}
