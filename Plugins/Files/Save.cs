using Agience.SDK;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Agents.Primary.Templates.Files
{
    internal class Save
    {
        [KernelFunction, Description("Save data to a file in the local filesystem.")]
        public Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}
