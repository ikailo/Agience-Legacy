using Agience.Client;

namespace Agience.Agents._Console.Templates
{
    public class GetInputFromUser : Template
    {
        public override Data? Description => "Get input from the user.";

        protected override async Task<Data?> Process(Runner runner, Data? data)
        {
            return await Task.Run(() =>
            {
                return Console.ReadLine() ?? string.Empty;
            });
        }

        protected override async Task<bool> Assess(Runner runner, Data? input = null)
        {
            return await Task.FromResult(true);

        }
    }
}