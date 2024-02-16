namespace Agience.Client.Templates.Default
{
    public class History : Template
    {
        public override Data? Description => "Return a collection of history items for a given time period.";
        public override string[]? InputKeys => new string[] {"startTimestamp","endTimestamp"};
        public override string[]? OutputKeys => new string[] {"timestamps", "history_items"};

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}