namespace Technologai.Templates
{
    public class InteractWithUser : Template
    {
        public InteractWithUser()
        {
            Id = "interact_with_user";
            Description = "Show a message to the user and then receive a text input from the user. Find, and then respond with, the best template response to the user's input.";
        }

        public override Task<bool> Assess(Information information) => Task.FromResult(true);

        public override async Task<Data?> Process(Information information)
        {
            await information.Publish("show_message_to_user", $"{information.Input} \r\n> ");

            var userInput = await information.Publish("get_input_from_user");

#if DEBUG

            if (userInput?.Raw?.StartsWith("DEBUG:") ?? false)
            {
                return await information.Publish("debug", userInput);
            }
#endif

            var bestTemplate = await information.Publish("get_best_template", userInput);

            return await information.Publish(bestTemplate?.Structured?["id"] ?? "input_to_output", userInput);

        }
    }
}