using NolowaNetwork.Models.Message;

namespace TestConsoleClientApp
{
    internal class TestMessage : NetMessageBase
    {
        public string Message { get; set; } = string.Empty;
    }
}
