using Agience.Client;

namespace Agience.Templates
{
    public class ShowMessageToUser : Template
    {   
        public ShowMessageToUser()
        {
            Description = "Show a message to the user.";            
        }

        public async override Task<Data?> Process(Data? data)
        {
            Console.Write($"{data}");
            return null;
        }
    }
}