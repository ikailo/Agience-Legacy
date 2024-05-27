using Agience.SDK;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Plugins.Primary.Text
{
    internal class CharacterLength
    {
        [KernelFunction, Description("Count the number of characters in the input.")]
        public Task<Data?> Process(Runner runner, Data? input = null)
        {
            return Task.FromResult<Data?>(input?.ToString()?.Length.ToString());
        }
    }
}
