using Agience.Client;

namespace Agience.Templates.Default
{
    public class Prompt : Template
    {
        public override Data? Description => throw new NotImplementedException();

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}