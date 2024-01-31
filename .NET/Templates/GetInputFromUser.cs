using Agience.Client;

namespace Agience.Templates
{
    public class GetInputFromUser : Template
    {
        public GetInputFromUser()
        {
            Description = "Receive a text input from the user.";
        }   

        public override async Task<Data?> Process(Data? data)
        {
            return await Task.Run(() =>
            {
                return new Data(Console.ReadLine() ?? string.Empty);
            });
        }
    }
}