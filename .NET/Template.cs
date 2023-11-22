namespace Agience
{
    public enum TemplateState
    {
        RESTING = 0,
        ASSESSING = 1,
        PROCESSING = 2        
    }
    public class Template 
    {
        public string? Id { get; set; }
        public Data? Description { get; set; }
        public string[]? InputKeys { get; set; }
        public string[]? OutputKeys { get; set; }
        public string? MemberId { get; set; }
        public virtual Task<bool> Assess(Information information) => Task.FromResult(false);
        public virtual Task<Data?> Process(Information information) => Task.FromResult((Data?)null);
    }
}