using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agience.SDK.Services
{
    internal class AgienceCredentialService
    {
        private readonly string _agentId;
        private readonly Authority _authority;
        private readonly Broker _broker;

        public AgienceCredentialService(
            string agentId, 
            Authority authority, 
            Broker broker
            )
        {
            _agentId = agentId;
            _authority = authority;
            _broker = broker;
        }

        public async Task<string> GetCredentialAsync()
        {
            
            return await _authority.GetCredentialAsync(_agentId);
        }

    }
}
