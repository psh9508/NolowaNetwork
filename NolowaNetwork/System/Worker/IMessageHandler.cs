using NolowaNetwork.Models.Message;

namespace NolowaNetwork.System.Worker
{
    public interface IMessageHandler
    {
        Task HandleAsync(dynamic message, CancellationToken cancellationToken);
    }
}
