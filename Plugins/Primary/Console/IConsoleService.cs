namespace Agience.Plugins.Primary._Console
{
    public interface IConsoleService
    {
        public Task<string?> ReadLineAsync();
        public Task WriteLineAsync(string message);
    }
}
