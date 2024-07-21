using NolowaNetwork.Models.Message;
using NolowaNetwork.System.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NolowaNetwork.System.Worker.RabbitWorker;

namespace NolowaNetwork.System
{
    public interface IMessageBroker
    {
        Task SendMessageAsync(NetMessageBase message, CancellationToken cancellationToken);
    }

    public class MessageBroker : IMessageBroker
    {
        private readonly IWorker _worker;
        private readonly IMessageCodec _codec;

        public MessageBroker(IWorker worker, IMessageCodec codec)
        {
            _worker = worker;
            _codec = codec;
        }

        public async Task SendMessageAsync(NetMessageBase message, CancellationToken cancellationToken)
        {
            var sendMessage = new NetSendMessage()
            {
                MessageType = message.GetType().Name,
                JsonPayload = _codec.EncodeAsJson((TestMessage)message), // T를 제네릭화 해야함
                Destination = message.Destination,
            };

            await _worker.QueueMessageAsync(ERabbitWorkerType.SENDER.ToString(), sendMessage, cancellationToken);
        }
    }
}
