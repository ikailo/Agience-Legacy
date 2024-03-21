using Agience.Client.Agience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Agience.Client
{
    public interface IAgentBuilder
    {
        IAgent Build();
    }
}