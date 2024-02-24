using Agience.Client;

namespace Agience.Agents.Primary.Templates.Text
{
    internal class CountWords : Template
    {
        public override Data? Description => "Count the number of words in the input.";

        protected override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}
