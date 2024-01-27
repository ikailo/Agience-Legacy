using Agience.Client.Model;

namespace Agience.Templates
{
    public class GetInputFromUser : Template
    {
        public GetInputFromUser()
        {
            Description  = "Receive a text input from the user.";
        }

        public override Task<bool> Assess(Information information) => Task.FromResult(true);

        public override async Task<Data?> Process(Information information)
        {
            return await Task.Run(() =>
            {
                return new Data(Console.ReadLine() ?? string.Empty);
            });
        }
    }
}