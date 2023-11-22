using Agience.Templates;

namespace Agience.Agents_Console
{
    internal class Program
    {
        private static Agent _agent;
        private static AppConfig _config = new AppConfig();
        private static bool _isStarted = true;

        internal static async Task Main(string[] args)
        {
            var authority = _config.Authority ?? throw new ArgumentNullException(nameof(_config.Authority));
            var instanceId = _config.InstanceId ?? throw new ArgumentNullException(nameof(_config.InstanceId));
            var instanceSecret = _config.InstanceSecret ?? throw new ArgumentNullException(nameof(_config.InstanceSecret));
            var agentId = _config.AgentId ?? throw new ArgumentNullException(nameof(_config.AgentId));

            try
            {
                Console.WriteLine("Loading...");

                _agent = new Agent(authority, instanceId, instanceSecret, agentId);
                _agent.LogMessage += LogMessage_callback;

                // Add local templates
                _agent.Catalog.Add(new GetInputFromUser());
                _agent.Catalog.Add(new InteractWithUser());
                _agent.Catalog.Add(new Debug(_agent));
                _agent.Catalog.Add(new ShowMessageToUser(ShowMessageToUser_callback));

                await _agent.Start();

                await _agent.PublishAsync("interact_with_user", InteractWithUser_callback, "Ready for Input");

                //await _agent.Prompt("Start a new conversation.", InteractWithUser_callback);
                                
                do { await Task.Delay(10); } while (_isStarted);

                await _agent.Stop();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        private async static Task InteractWithUser_callback(Data? output)
        {
            if (output?.Raw?.Equals("quit", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                _isStarted = false;

                Console.WriteLine($"{_agent?.Name} Shutting Down");
            }
            else
            {
                await _agent.PublishAsync("interact_with_user", InteractWithUser_callback, output) ;
                //await _agent.Prompt("Continue the conversation.", InteractWithUser_callback);
            }
        }

        private static void ShowMessageToUser_callback(string? message)
        {
            Console.Write($"{(string.IsNullOrEmpty(message) ? string.Empty : $"{message}")}");
        }

        private static void LogMessage_callback(object? sender, string message)
        {
            Console.WriteLine($"{_agent?.Name ?? "Interaction.Local"} | {message}");
        }
    }
}