using Agience.Client.Agience;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Client.Templates.Default
{
    public class Context //: Template
    {
        [KernelFunction, Description("Show a message to the user via the console.")]
        public Task<Data?> Process(Runner runner, Data? input = null)
        {
            Console.WriteLine("Default Context template has been called. //TODO: Implement.");

            return Task.FromResult<Data?>(null);
        }
    }
}