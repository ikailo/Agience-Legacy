using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Client
{
    // TODO: Implement layer processing. Check link for more info.
    // https://github.com/daveshap/ACE_Framework/blob/main/publications/Conceptual%20Framework%20for%20Autonomous%20Cognitive%20Entities%20(ACE).pdf
    // https://github.com/daveshap/ACE_Framework/blob/main/ACE_PRIME/HelloAF/src/ace/resources/core/hello_layers/prompts/templates/ace_context.md

    public abstract class Template
    {
        public string Id { get; }
        public abstract Data? Description { get; } // Description is the template identifier. Future: Searchable, Inferable, Unique, Persist.
        public virtual string[]? InputKeys { get; protected set; } // TODO: Use a Format string instead?
        public virtual string[]? OutputKeys { get; protected set; } // TODO: Use a Format string instead?

        internal Agent? Agent;

        public Template()
        {
            Id = GetType().FullName ?? throw new ArgumentNullException("Type.FullName");
        }

        protected internal virtual Task<bool> Assess(Runner runner, Data? input = null) => Task.FromResult(true);
       
        protected internal abstract Task<Data?> Process(Runner runner, Data? input = null);
        protected internal virtual Task<string?> Context(Runner runner, Data? input = null, Data? output = null) => Task.FromResult<string?>(null);

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