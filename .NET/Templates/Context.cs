namespace Agience.Client.Templates.Default
{
    public class GetContext : Template
    {
        public override Data? Description => "Get the current context.";

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }

    public class AddContext : Template
    {
        public override Data? Description => "Add to the current context.";

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}