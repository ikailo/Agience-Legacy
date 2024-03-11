using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Experimental.Agents;

namespace Agience.Client
{
    internal class AgentThread : IAgentThread
    {
        public string Id => throw new NotImplementedException();

        public Task<IChatMessage> AddUserMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IChatMessage> InvokeAsync(IAgent agent, KernelArguments? arguments = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IChatMessage> InvokeAsync(IAgent agent, string userMessage, KernelArguments? arguments = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
