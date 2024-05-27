using GuerrillaNtp;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Agience.SDK
{
    public class Information
    {
        public string Id { get; }
        public string? ParentInformationId { get; internal set; }

        public Data? Input { get; internal set; }
        public string? InputAgentId { get; internal set; }
        public string? InputTimestamp { get; internal set; }

        public Data? Output { get; internal set; }
        public string? OutputAgentId { get; internal set; }
        public string? OutputTimestamp { get; internal set; }


        public string? TemplateId { get; internal set; }
        public Data? Transformation { get; internal set; }


        // TODO: History, Signatures, ReadOnly fields ?
        // TODO: Account for partial information (e.g. input only, output only, etc.) when in transit. For performance.

        public Information()
        {
            using (var sha256 = SHA256.Create())
            {
                // Using standard 256 bit urlsafe id.
                // TODO: Consider composing it from related factors - agentId, timestamp, parentId, etc.. for traceability.                
                Id = Base64UrlEncoder.Encode(sha256.ComputeHash(Guid.NewGuid().ToByteArray()));
            }
        }
        
        // TODO: Is a separate constructor for deserialization necessary?
        [JsonConstructor]
        public Information( string id,
                            string? parentInformationId,
                            Data? input,
                            string? inputAgentId,
                            string? inputTimestamp,
                            Data? output,
                            string? outputAgentId,
                            string? outputTimestamp,
                            string? templateId,
                            Data? transformation                            
                            )
        {
            Id = id;
            ParentInformationId = parentInformationId;            
            Input = input;
            InputAgentId = inputAgentId;
            InputTimestamp = inputTimestamp;
            Output = output;
            OutputAgentId = outputAgentId;
            OutputTimestamp = outputTimestamp;
            Transformation = transformation;
            TemplateId = templateId;
        }
    }
}
