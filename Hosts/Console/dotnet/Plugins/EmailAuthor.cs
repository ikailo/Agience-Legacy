using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Agience.Agents._Console.Plugins
{
    public class EmailAuthor
    {

        [KernelFunction]
        [Description("Returns back the required steps necessary to author an email.")]
        [return: Description("The list of steps needed to author an email")]
        public async Task<string> GenerateRequiredStepsAsync(
          Kernel kernel,
          [Description("A 2-3 sentence description of what the email should be about")] string topic,
          [Description("A description of the recipients")] string recipients
      )
        {
            // Prompt the LLM to generate a list of steps to complete the task
            var result = await kernel.InvokePromptAsync(
                $"I'm going to write an email to {recipients} about {topic} on behalf of a user." +
                $"Before I do that, can you succinctly recommend the top 3 steps I should take in a numbered list?" +
                $"I want to make sure I don't forget anything that would help my user's email sound more professional.",
                new() {
                    { "topic", topic },
                    { "recipients", recipients }
                });

            // Return the plan back to the agent
            return result.ToString();
        }

        [KernelFunction]
        [Description("Sends an email to a recipient.")]
        public Task SendEmailAsync(
            [FromKernelServices] IConsoleService console,
            [Description("Semicolon delimitated list of emails of the recipients")] string recipientEmails,
            string subject,
            string body
        )
        {
            // Add logic to send an email using the recipientEmails, subject, and body
            // For now, we'll just print out a success message to the console

            console.WriteLine($"* Sent Email *\r\nTo: {recipientEmails}\r\nSubject: {subject}\r\nBody: {body}\r\n* * *");

            return Task.CompletedTask;
        }
    }
}
