using NolowaNetwork.Models.Message;

namespace TestConsoleApp
{
    public class TestMessage : NetMessageBase
    {
        public string Message { get; set; } = string.Empty;
    }
}
