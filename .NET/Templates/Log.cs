namespace Agience.Client.Templates.Default
{
    public class Log : Template
    {
        public override Data? Description => "Write entries to the Agency log.";

        public override string[] InputKeys => new[] { "timestamp", "agent_id", "level", "message" };

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            Console.WriteLine($"LOG: {input}");
            
            return Task.FromResult<Data?>(null);
        }
    }
}