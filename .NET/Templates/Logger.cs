using Agience.Client;

namespace Agience.Templates.Default
{
    public class Logger : Template
    {
        public override Data? Description => "Write entries to the Agency log.";

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}