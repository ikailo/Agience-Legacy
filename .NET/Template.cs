namespace Agience.Client
{
    // TODO: Implement layer processing. Check link for more info.
    // https://github.com/daveshap/ACE_Framework/blob/main/publications/Conceptual%20Framework%20for%20Autonomous%20Cognitive%20Entities%20(ACE).pdf

    public abstract class Template
    {
        public string Id { get; }
        public abstract Data? Description { get; } // Description is the template identifier. Future: Searchable, Inferable, Unique, Persist.
        public virtual string[]? InputKeys { get; protected set; }
        public virtual string[]? OutputKeys { get; protected set; }

        internal Agent? Agent;

        public Template()
        {
            Id = GetType().FullName ?? throw new ArgumentNullException("Type.FullName");
        }

        protected internal virtual Task<bool> Assess(Runner runner, Data? input = null) => Task.FromResult(true);
        protected internal abstract Task<Data?> Process(Runner runner, Data? input = null);     

        internal Model.Template ToAgienceModel()
        {
            return new Model.Template()
            {
                Id = Id,
                Description = Description,
                InputKeys = InputKeys,
                OutputKeys = OutputKeys,
                AgentId = Agent?.Id
            };
        }       
    }
}