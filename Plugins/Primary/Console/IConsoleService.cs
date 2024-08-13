namespace Agience.Plugins.Primary._Console
{
    public interface IConsoleService
    {
        Task<string?> ReadLineAsync();
        Task WriteLineAsync(string message);
    }
}
