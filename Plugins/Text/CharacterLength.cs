using Agience.Client;

namespace Agience.Agents.Primary.Templates.Text
{
    internal class CharacterLength : Template
    {
        public override Data? Description => "Count the number of characters in the input.";

        protected override Task<Data?> Process(Runner runner, Data? input = null)
        {
            return Task.FromResult<Data?>(input?.ToString()?.Length.ToString());
        }
    }
}
