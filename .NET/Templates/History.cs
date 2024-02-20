namespace Agience.Client.Templates.Default
{
    public class History : Template
    {
        public override Data? Description => "Get a collection of information items for a given time period.";
        public override string[]? InputKeys => new string[] {"startTimestamp","endTimestamp"};
        public override string[]? OutputKeys => new string[] {"information_items"};

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            Console.WriteLine("Default History template has been called. //TODO: Implement.");

            return Task.FromResult<Data?>(null);
        }
    }
}