namespace Agience.Agents_Console.Plugins
{
    public interface IConsoleService
    {
        public Task<string?> ReadLineAsync();
        public void Write(string message);
    }
}
