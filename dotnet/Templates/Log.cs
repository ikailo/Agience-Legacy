namespace Agience.Client.Templates.Default
{
    public class Log : Template
    {
        public override Data? Description => "Write entries to the Agency log.";

        public override string[] InputKeys => new[] { "timestamp", "agent_id", "agent_name", "level", "message" };

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            var agentId = input?["agent_id"];
            var agentName = input?["agent_name"];
            var timestamp = input?["timestamp"];
            var level = input?["level"];
            var message = input?["message"];

            Console.WriteLine($"{timestamp} | default-{level} | {agentName} | {message}");
            
            return Task.FromResult<Data?>(null);
        }
    }
}