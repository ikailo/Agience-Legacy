using Agience.Client;

namespace Agience.Agents.Primary.Templates.OpenAI
{
    internal class SelectTemplate : Template
    {
        public override Data? Description => throw new NotImplementedException();

        protected override Task<Data?> Process(Runner runner, Data? input = null)
        {
            throw new NotImplementedException();
        }
    }
}


/*
var prompt = $"Your response MUST be a compliant machine-readable JSON document.\r\n\r\n" +
$"{JsonConvert.SerializeObject(choose_ability)}" +
$"\r\n\r\nGiven the list of abilities provided, specify which one you would like to use to respond to the input. " +
$"Your response should consist of a single JSON object with the name of the selected ability. For example: {information.Template.SampleJsonOut}";
*/