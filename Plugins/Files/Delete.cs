using Agience.Client;

namespace Agience.Agents.Primary.Templates.Files
{
    internal class Delete : Template
    {
        public override Data? Description => "Delete a file on the local filesystem";
        public override string[] InputKeys => ["file_path"];

        protected override Task<Data?> Process(Runner runner, Data? input = null)
        {
            var fileName = input?["file_name"] ?? throw new ArgumentNullException("file_name");

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            return Task.FromResult<Data?>(null);
        }
    }
}
