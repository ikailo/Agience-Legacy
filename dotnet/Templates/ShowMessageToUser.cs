using Agience.Client;

namespace Agience.Agents._Console.Templates
{
    public class ShowMessageToUser : Template
    {   
        public override Data? Description => "Show a message to the user.";        

        protected override Task<Data?> Process(Runner runner, Data? input)
        {
            Console.Write($"{input}");
            
            return Task.FromResult<Data?>(null);
        }
    }
}