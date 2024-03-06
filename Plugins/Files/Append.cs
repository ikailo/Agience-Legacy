using Agience.Client;

namespace Agience.Agents.Primary.Templates.Files
{
    internal class Append : Template
    {
        public override Data? Description => "Append text to file in the local filesystem.";
        public override string[] InputKeys => ["file_path", "content"];

        protected override async Task<Data?> Process(Runner runner, Data? input = null)
        {
            using (var writer = new StreamWriter(input?["file_path"] ?? throw new ArgumentNullException("file_path")))
            {
                await writer.WriteAsync(input?["content"]);
            }
            return null;
        }
    }
}
