using Agience.Client;

namespace Agience.Templates
{
    public class ShowMessageToUser : Template
    {   
        public override Data? Description => "Show a message to the user.";        

        protected override Task<Data?> Process(Data? data)
        {
            Console.Write($"{data}");
            
            return Task.FromResult<Data?>(null);
        }
    }
}