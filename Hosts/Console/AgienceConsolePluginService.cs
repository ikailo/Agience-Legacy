using Agience.Plugins.Primary._Console;

namespace Agience.Hosts._Console
{
    internal class AgienceConsolePluginService : IConsoleService
    {
        private readonly StreamReader _inputReader = new(Console.OpenStandardInput());
        private static readonly object _writeLock = new object();
        private static readonly object _readLock = new object();

        public Task<string?> ReadLineAsync()
        {
            lock (_readLock)
            {
                return _inputReader.ReadLineAsync();
            }
        }

        public Task WriteLineAsync(string message)
        {
            lock (_writeLock)
            {
                Console.WriteLine(message);
            }
            return Task.CompletedTask;
        }
    }
}