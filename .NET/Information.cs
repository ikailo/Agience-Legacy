using GuerrillaNtp;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Agience.Client
{
    public class Information
    {
        public string Id { get; }
        public string? ParentInformationId { get; internal set; }
        public string? InputAgentId { get; internal set; }
        public string? OutputAgentId { get; internal set; }
        public string? TemplateId { get; internal set; }
        public Data? Input { get; internal set; }
        public Data? Transformation { get; internal set; }
        public Data? Output { get; internal set; }

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

        public Information(string parentInformationId, string inputAgentId, Data? input = null, Template? template = null)
            : this()
        {
            ParentInformationId = parentInformationId;
            InputAgentId = inputAgentId;
            Input = input;
            Transformation = template?.Description;
            TemplateId = template?.Id;

        }

        [JsonConstructor]
        public Information(string id, 
                            string? parentInformationId,
                            string? inputAgentId,
                            string? outputAgentId,
                            string? templateId,
                            Data? input = null, 
                            Data? transformation = null,                             
                            Data? output = null
                            )            
        {
            Id = id;
            ParentInformationId = parentInformationId;
            InputAgentId = inputAgentId;
            OutputAgentId = outputAgentId;
            TemplateId = templateId;
            Input = input;
            Transformation = transformation;
            Output = output;
        }
    }
}
