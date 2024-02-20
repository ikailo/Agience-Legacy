namespace Agience.Client.Templates.Default
{
    public class Prompt : Template
    {
        public override Data? Description => "Receive and process a prompt from the User.";

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            Console.WriteLine("Default Prompt template has been called. //TODO: Implement.");
            
            return Task.FromResult<Data?>(null);
        }
    }
}