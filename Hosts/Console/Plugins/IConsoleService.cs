namespace Agience.Hosts._Console.Plugins
{
    public interface IConsoleService
    {
        Task<string?> ReadLineAsync();
        void Write(string message);
        void WriteLine(string message);
    }
}
