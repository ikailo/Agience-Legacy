namespace Agience.Client.Model
{
    public class Agency : Agience.Model.Agency
    {

        // Agency
        // Knows about its agents        
                
        public new List<Agent> Agents { get; set; } = new();

        public event EventHandler<Message>? MessageReceived;                

    }
}
