namespace Agience
{
    public class Authority
    {
        public string AuthUri { get; private set; } = "https://auth.technologai.com";        
        public string BrokerUri { get; private set; } = "https://broker.technologai.com";
        
        public Authority(string authUri)
        {
            // TODO: Connect to an OIDC Discovery Endpoint and get the authoratative values

            if (authUri != "https://auth.technologai.com") { throw new NotImplementedException(); }

            this.AuthUri = authUri;
        }
    }
}
