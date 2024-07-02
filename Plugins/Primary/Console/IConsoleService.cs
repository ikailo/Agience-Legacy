namespace Agience.Plugins.Primary._Console
{
    public interface IConsoleService
    {
        Task<string?> ReadLineAsync();
        void Write(string message);
        void WriteLine(string message);
    }
}
