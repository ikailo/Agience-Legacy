namespace Agience.Client.Templates.Default
{
    public class Context : Template
    {
        public override Data? Description => "Interact with the current context.";

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            Console.WriteLine("Default Context template has been called. //TODO: Implement.");

            return Task.FromResult<Data?>(null);
        }
    }
}