namespace Agience.Templates
{
    public class GetInputFromUser : Template
    {
        public GetInputFromUser()
        {
            Id = "get_input_from_user";
            Description = "Receive a text input from the user.";
        }

        public override Task<bool> Assess(Information information) => Task.FromResult(true);

        public override async Task<Data?> Process(Information information)
        {
            return await Task.Run(() =>
            {
                return new Data(Console.ReadLine() ?? string.Empty);
            });
        }
    }
}