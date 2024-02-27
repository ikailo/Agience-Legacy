using Agience.Client;

namespace Agience.Agents._Console.Templates
{
    public class GetInputFromUser : Template
    {
        public override Data? Description => "Wait for the user to respond.";

        private readonly StreamReader _inputReader = new(Console.OpenStandardInput());

        protected override async Task<Data?> Process(Runner runner, Data? data)
        {
            return await _inputReader.ReadLineAsync() ?? string.Empty; ;
        }
    }
}