using NolowaNetwork.Models.Message;

namespace NolowaNetwork.System.Worker
{
    public interface IMessageHandler
    {
        Task HandleAsync(NetMessageBase message, CancellationToken cancellationToken);
    }
}
