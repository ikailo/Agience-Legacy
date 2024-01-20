using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using Agience.Model;
using System.Security.Claims;
using System.Text;

namespace Agience.Client.MQTT.Model
{
    public class Instance : Agience.Model.Instance
    {
        public List<Agent> Agents { get; set; } = new List<Agent>();
        public Catalog Catalog { get; set; } = new Catalog();
        public bool IsStarted { get; set; }

        //private string? _token;

        private Authority _authority;

        //private string _clientId;
        private string _clientSecret;

        //private Identity _identity;
        private Broker _broker;
        //private Catalog _catalog;

        private string? _access_token;

        public event Action<object?, string> LogMessage;

        public Instance(string authorityUri, string clientId, string clientSecret)
        {
            _authority = new Authority(authorityUri);
            //_identity = new Identity(authorityUri,  clientId, clientSecret);

            Id = clientId;
            _clientSecret = clientSecret;


            _broker = new Broker();
        }


        public Agent CreateAgent(Action<Agent> configure)
        {
            throw new NotImplementedException();
            /*
            var newAgent = new Agent();
            configure(newAgent);
            Agents.Add(newAgent);
            return newAgent;*/
        }

        private async Task Receive(Template? template)
        {
            if (template?.Id != null && template.InstanceId != Id)
            {
                await Logger.Write($"{template.InstanceId} {template.Id} template receive");

                Catalog.Add(agent => template);
            }
        }

        public async Task Start()
        {
            await _authority.InitializeAsync();

            await Authenticate();

            //await Logger.Write($"Authenticated");

            if (_access_token == null) { throw new Exception("Access Token is null"); }

            await _broker.ConnectAsync(_authority.BrokerHost, _access_token);

            await Logger.Write($"Connected");

            //await _broker.SubscribeAsync(_identity.InstanceSubscribeTopic);

            //await Logger.Write($"Subscribed {_identity.InstanceSubscribeTopic}");

            await _broker.Send(new Status(Id));

            //await Task.Delay(5000); // Wait here for a bit to sync up Templates
        }

        public async Task Stop()
        {
            await _broker.DisconnectAsync();
        }

        internal async Task Authenticate()
        {
            // TODO: Consider using IHttpClientFactory or a shared HttpClient instance
            using (var httpClient = new HttpClient())
            {
                var basicAuthHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Id}:{_clientSecret}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthHeader);

                var parameters = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },                    
                    // { "audience", "/connect" },
                    // { "version", version },
                    { "scope", "connect" }
                };

                var content = new FormUrlEncodedContent(parameters);
                var httpResponse = await httpClient.PostAsync(_authority.TokenEndpoint, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var tokenResponse = await httpResponse.Content.ReadFromJsonAsync<TokenResponse>();

                    if (tokenResponse?.access_token != null)
                    {
                        // Directly use the access token
                        _access_token = tokenResponse.access_token;
                        // Do any additional processing if needed
                        return;
                    }
                }

                // Log the error or handle it as necessary
                throw new HttpRequestException("Unauthorized", null, httpResponse.StatusCode);
            }
        }

        internal class TokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }

    }
}
