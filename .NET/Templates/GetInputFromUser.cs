using Agience.Client;

namespace Agience.Agents._Console.Templates
{
    public class GetInputFromUser : Template
    {
        public override Data? Description => "Get input from the user.";

        private readonly StreamReader _inputReader = new(Console.OpenStandardInput());

        protected override async Task<Data?> Process(Runner runner, Data? data)
        {
            return await _inputReader.ReadLineAsync() ?? string.Empty; ;
        }
    }
}