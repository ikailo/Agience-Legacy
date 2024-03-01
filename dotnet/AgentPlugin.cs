using Microsoft.SemanticKernel;
namespace Agience.Client
{
    internal abstract class AgentPlugin : KernelPlugin
    {
        public AgentPlugin(string name, string? description = null)
            : base(name, description)
        {
        }

        internal abstract Agent Agent { get; }

        public async Task<string> InvokeAsync(string input, CancellationToken cancellationToken = default)
        {
            return await this.InvokeAsync(input, arguments: null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> InvokeAsync(string input, KernelArguments? arguments, CancellationToken cancellationToken = default)
        {
            arguments ??= new KernelArguments();

            arguments["input"] = input;

            var result = await this.First().InvokeAsync(this.Agent.Kernel, arguments, cancellationToken).ConfigureAwait(false);
            var response = result.GetValue<AgentResponse>()!;

            return response.Message;
        }
    }
}
