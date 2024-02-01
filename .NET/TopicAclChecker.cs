namespace Agience.Client
{
    public class TopicAclChecker
    {
        private const string ALL = "0";
        private const string NONE = "-";
        private const string ANY_EXCLUSIVE = "*";
        private const string ANY_INCLUSIVE = "+";
        private const string QUERY = "?";

        private const int READ = 1;
        private const int WRITE = 2;
        private const int READ_WRITE = 3;
        private const int SUBSCRIBE = 4;

        private Func<string, string?, string?, string?, Task<bool>> _areAgentsRelatedAndSameInstance;

        public TopicAclChecker(Func<string, string?, string?, string?, Task<bool>> areAgentsRelatedAndSameInstance)
        {
            _areAgentsRelatedAndSameInstance = areAgentsRelatedAndSameInstance;
        }

        // TODO: Return better status codes, logging.

        public async Task<bool> CheckAccessControl(AclCheckRequest? aclRequest, string? instanceId, List<string> roles, string? authorityId)
        {
            if (aclRequest == null || string.IsNullOrWhiteSpace(aclRequest.topic) || aclRequest.acc == 0)
            {
                return false;

                //return BadRequest("Invalid ACL request.");
            }

            if (aclRequest.acc == READ_WRITE)
            {
                return false;

                //return Unauthorized("READ_WRITE access is not permitted.");
            }

            var masks = GetUserMasks(aclRequest.acc, roles, authorityId, instanceId);

            if (await IsTopicAllowed(aclRequest.topic, masks, instanceId, aclRequest.acc))
            {
                return true;
                //return Ok();
            }

            return false;
            //return Unauthorized();
        }

        private List<string> GetUserMasks(int accessType, List<string> roles, string? authorityId, string? instanceId)
        {
            bool isAuthority = roles.Contains("authority");
            bool isInstance = roles.Contains("instance");

            List<string> masks = new();

            if (isAuthority && authorityId != null)
            {
                if (accessType == READ || accessType == SUBSCRIBE)
                {
                    masks.Add($"{ANY_EXCLUSIVE}/{authorityId}/{NONE}/{NONE}/{NONE}"); // Any -> Authority
                }
                if (accessType == WRITE)
                {
                    masks.Add($"{NONE}/{authorityId}/{ANY_INCLUSIVE}/{NONE}/{NONE}"); // Authority -> Any or All Instances                    
                }
            }

            if (isInstance && instanceId != null)
            {
                if (accessType == READ || accessType == SUBSCRIBE)
                {
                    masks.Add($"{NONE}/{authorityId}/{ALL}/{NONE}/{NONE}"); // Authority -> All Instances
                    masks.Add($"{NONE}/{authorityId}/{instanceId}/{NONE}/{NONE}"); // Authority -> Instance
                    masks.Add($"{ANY_EXCLUSIVE}/{authorityId}/{NONE}/{QUERY}/{NONE}"); // Any -> Agency
                    masks.Add($"{ANY_EXCLUSIVE}/{authorityId}/{NONE}/{NONE}/{QUERY}"); // Any -> Agent
                }
                if (accessType == WRITE)
                {
                    masks.Add($"{instanceId}/{authorityId}/{NONE}/{NONE}/{NONE}"); // Instance -> Authority
                    masks.Add($"{QUERY}/{authorityId}/{NONE}/{QUERY}/{NONE}"); // Agent -> Agency
                    masks.Add($"{QUERY}/{authorityId}/{NONE}/{NONE}/{QUERY}"); // Agent -> Agent
                }
            }
            return masks;
        }

        private async Task<bool> IsTopicAllowed(string topic, List<string> masks, string? instanceId, int accessType)
        {
            foreach (var mask in masks)
            {
                if (mask.Contains(QUERY)) // TODO: Subject to "?" injection attack.
                {
                    if (instanceId == null) throw new ArgumentNullException(nameof(instanceId));

                    if (await CheckQueryMaskAsync(topic, mask, instanceId, accessType)) { return true; }
                }
                else
                {
                    if (CheckMask(topic, mask, accessType)) { return true; }
                }
            }

            return false;
        }

        private async Task<bool> CheckQueryMaskAsync(string topic, string mask, string instanceId, int accessType)
        {
            var topicParts = topic.Split('/');
            var maskParts = mask.Split('/');

            if (!IsValidTopicAndMask(topicParts, maskParts)) return false;

            var sourceAgentId = maskParts[0] == QUERY ? null : topicParts[0];
            var agencyId = maskParts[3] == QUERY ? null : topicParts[3];
            var targetAgentId = maskParts[4] == QUERY ? null : topicParts[4];

            if (accessType == SUBSCRIBE)
            {
                if (topicParts[0] != ANY_INCLUSIVE) { return false; }
                sourceAgentId = null;
            }

            return await _areAgentsRelatedAndSameInstance(instanceId, agencyId, sourceAgentId, targetAgentId);
        }

        private bool CheckMask(string topic, string mask, int accessType)
        {
            var topicParts = topic.Split('/');
            var maskParts = mask.Split('/');

            if (!IsValidTopicAndMask(topicParts, maskParts)) return false;

            if (topicParts[0] == null || maskParts[0] == null) { return false; }

            // First part is the sender id. Read from any sender. Otherwise the sender id must match the claims.
            if (accessType != READ)
            {
                if (accessType == SUBSCRIBE && topicParts[0] != ANY_INCLUSIVE) { return false; }
                if (accessType == WRITE && topicParts[0] != maskParts[0]) { return false; }
            }

            for (int i = 1; i < maskParts.Length; i++)
            {
                switch (maskParts[i])
                {
                    case ANY_INCLUSIVE:
                        continue;
                    case ANY_EXCLUSIVE when topicParts[i] != ALL:
                        continue;
                    case ALL when topicParts[i] == ALL:
                        continue;
                    case NONE when topicParts[i] == NONE:
                        continue;
                    case var m when m != topicParts[i]:
                        return false;
                }
            }
            return true;
        }

        private bool IsValidTopicAndMask(string[] topicParts, string[] maskParts)
        {
            return topicParts.Length == 5 && maskParts.Length == 5;
        }

        public class AclCheckRequest
        {
            public int acc { get; set; }
            public string clientid { get; set; } = string.Empty;
            public string topic { get; set; } = string.Empty;
        }

        // For logging
        private string PrintConst(int value)
        {
            return value switch
            {
                0 => "0",
                1 => nameof(READ),
                2 => nameof(WRITE),
                3 => nameof(READ_WRITE),
                4 => nameof(SUBSCRIBE),
                _ => "UNKNOWN"
            };
        }
    }
}
