using Agience.Client;

namespace Agience.Agents.Primary.Templates.Jira
{
    internal class Work : Template
    {
        public override Data? Description => throw new NotImplementedException();

        protected override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}
