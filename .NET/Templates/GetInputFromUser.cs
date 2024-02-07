using Agience.Client;

namespace Agience.Templates
{
    public class GetInputFromUser : Template
    {
        public override Data? Description => "Get input from the user.";

        protected override async Task<Data?> Process(Data? data)
        {
            return await Task.Run(() =>
            {
                return Console.ReadLine() ?? string.Empty;
            });
        }

        protected override async Task<bool> Assess(Data? input = null)
        {
            return await Task.FromResult(true);

        }
    }
}