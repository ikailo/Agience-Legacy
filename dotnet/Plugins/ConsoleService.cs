namespace Agience.Agents_Console.Plugins
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
    }
}
