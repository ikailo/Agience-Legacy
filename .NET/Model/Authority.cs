namespace Agience.Client.MQTT.Model
{
    public class Authority //: Agience.Model.Authority
    {
        public string AuthorityUri { get; private set; } // "https://authority.agience.ai";        
        public string BrokerHost { get; }

        public Authority(string authorityUri)
        {
            AuthorityUri = authorityUri;
            BrokerHost = new Uri(authorityUri.Replace("authority.", "broker.")).Host; // TODO: Get from OIDC
        }
    }
}
