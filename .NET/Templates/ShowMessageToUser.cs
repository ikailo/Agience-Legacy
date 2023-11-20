namespace Technologai.Templates
{
    public class ShowMessageToUser : Template
    {
        internal event Action<string>? Message;

        public ShowMessageToUser(Action<string> messageCallback)
        {
            Id = "show_message_to_user";
            Description = "Show a message to the user.";
            Message += messageCallback;
        }

        public override Task<bool> Assess(Information information) => Task.FromResult(true);

        public override Task<Data?> Process(Information information)
        {
            Message?.Invoke(information?.Input?.Raw ?? string.Empty);
            return Task.FromResult((Data?)null);
        }
    }
}