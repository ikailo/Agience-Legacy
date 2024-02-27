namespace Agience.Client.Templates.Default
{
    public class Prompt : Template
    {
        public override Data? Description => "Receive a prompt from the User. Respond with a default message indicating the data has not been processed.";

        protected internal override Task<Data?> Process(Runner runner, Data? input = null)
        {
            string response = "The prompt template default cannot process input data. Please add a new prompt template default to the Agency.";
            
            return Task.FromResult<Data?>(response);
        }
    }
}