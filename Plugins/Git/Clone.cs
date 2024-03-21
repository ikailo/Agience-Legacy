using Agience.Client;
using Agience.Client.Agience;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Agents.Primary.Templates.Git
{
    internal class Clone
    {
        [KernelFunction, Description("Clone a git repository.")]
        public Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}
