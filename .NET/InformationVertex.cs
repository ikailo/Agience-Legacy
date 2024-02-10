namespace Agience.Client
{
    public class InformationVertex
    {

        // TODO: Need to make Data IComparable/IEquatable for this to work.
        public string? Id { get; internal set; }        
        public Data? Input { get; internal set; }
        public DateTime? InputTimestamp { get; internal set; }        
        public Data? Output { get; internal set; }        
        public DateTime? OutputTimestamp { get; internal set; }
        public Data? Transformation { get; internal set; }        
        public string? TemplateId { get; internal set; } // TODO: This should be implict to the Transformation.
    }
}
