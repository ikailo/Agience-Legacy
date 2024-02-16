namespace Agience.Client.Templates.Default
{
    public class Log : Template
    {
        public override Data? Description => "Write entries to the Agency log.";

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}