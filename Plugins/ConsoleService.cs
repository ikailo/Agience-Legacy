namespace Agience.Hosts._Console.Plugins
{
    internal class ConsoleService : IConsoleService
    {
        private readonly StreamReader _inputReader = new(Console.OpenStandardInput());

        public Task<string?> ReadLineAsync()
        {
            return _inputReader.ReadLineAsync();
        }

        public void Write(string message)
        {
            Console.Write(message);
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
