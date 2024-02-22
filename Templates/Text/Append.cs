using Agience.Client;

namespace Agience.Agents.Primary.Templates.Text
{
    internal class Append : Template
    {
        public override Data? Description => throw new NotImplementedException();

        protected override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}
