using Agience.SDK;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Agents.Primary.Templates.Text
{
    internal class CountWords 
    {   

        [KernelFunction, Description("Count the number of words in the input.")]
        public Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}
