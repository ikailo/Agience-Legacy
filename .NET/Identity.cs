using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Security.Claims;

namespace Agience
{
    public class Identity
    {
        public string? Name { get; private set; }
        public string AgentId { get; private set; }        
        public string? AgencyId { get; private set; }
        internal Authority Authority { get; private set; }
        internal string InstanceId { get; private set; }
        internal string InstanceSecret { get; private set; }        
        
        internal Dictionary<string, string> Tokens = new Dictionary<string, string>();
        internal string PublishMask => $"{AgencyId}/+";
        internal string SubscribeMemberMask => $"{AgencyId}/{AgentId}";
        internal string SubscribeAgencyMask => $"{AgencyId}/0";        

        public Identity(string authority, string instanceId, string instanceSecret, string agentId)
        {
            Authority = new Authority(authority);
            InstanceId = instanceId;
            InstanceSecret = instanceSecret;
            AgentId = agentId;
        }

        internal async Task Authenticate(string audience, string version = "1.0")
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Base64UrlEncoder.Encode($"{InstanceId}:{InstanceSecret}"));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var parameters = new Dictionary<string, string>();
                parameters.Add("grant_type", "client_credentials");                
                parameters.Add("audience", audience);
                parameters.Add("version", version);
                parameters.Add("scope", $"agent_id:{AgentId}");

                var endpoint = Authority?.AuthUri + Authority?.TokenApi;

                var httpResponse = await httpClient.PostAsJsonAsync(endpoint, parameters);

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var tokenResponse = await httpResponse.Content.ReadFromJsonAsync<TokenResponse>();

                    if (tokenResponse != null)
                    {
                        foreach (Claim claim in new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse.access_token).Claims)
                        {
                            if (claim.Type == "agency_id")
                            {
                                AgencyId = claim.Value;
                            }
                            if (claim.Type == "name")
                            {
                                Name = claim.Value;
                            }
                            if (claim.Type == "aud")
                            {
                                Tokens[claim.Value] = tokenResponse.access_token;
                            }
                        }
                        return;
                    }
                }
                throw new HttpRequestException("Unauthorized", null, httpResponse.StatusCode);
            }
        }
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

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }
    }
}
