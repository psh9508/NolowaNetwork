using NolowaNetwork.Models.Message;

namespace TestConsoleApp
{
    internal class ResponseMessage : NetMessageBase
    {
        public string Message { get; set; } = string.Empty;
    }
}
