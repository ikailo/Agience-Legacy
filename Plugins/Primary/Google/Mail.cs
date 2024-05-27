using Agience.SDK;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Plugins.Primary.Google
{
    internal class Mail
    {

        [KernelFunction, Description("Send an email.")]
        public Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}
