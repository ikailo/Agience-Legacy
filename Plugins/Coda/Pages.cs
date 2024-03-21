using Agience.Client;
using Agience.Client.Agience;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Agents.Primary.Templates.Coda
{
    internal class Pages //: Template
    {
        [KernelFunction, Description("Get a page by its ID.")]
        public Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}
