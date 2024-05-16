using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Client.Templates.Default
{
    public class Echo
    {   

        [KernelFunction, Description("Echo the input.")]    
     public Task<Data?> Process(Runner runner, Data? input = null)
        {
            return Task.FromResult(input);
        }
    }
}