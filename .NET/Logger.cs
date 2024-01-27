namespace Agience.Client
{
    internal static class Logger
    {
        internal static Task Write(string v)
        {
            throw new NotImplementedException();
        }


        public async static Task WriteLog(string message)
        {
            /*
            if (_mqtt.IsConnected && _catalog.ContainsKey(LOG_MESSAGE_TEMPLATE_ID) && _catalog[LOG_MESSAGE_TEMPLATE_ID].AgentId != null && _catalog[LOG_MESSAGE_TEMPLATE_ID].AgentId != Id)
            {
                await _broker.PublishAsync(LOG_MESSAGE_TEMPLATE_ID, null, $"{Name?.PadRight(21)} | {message}");
            }
            else
            {
                LogMessage?.Invoke(this, message);
            }
            */
        }
    }
}
