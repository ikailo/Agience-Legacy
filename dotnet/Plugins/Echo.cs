namespace Agience.Client.Templates.Default
{
    public class Echo : Template
    {
        public override Data? Description => "Echo the input.";

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            return Task.FromResult(input);
        }
    }
}