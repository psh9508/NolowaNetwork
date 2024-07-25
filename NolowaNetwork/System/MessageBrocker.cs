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
        Task SendMessageAsync<T>(T message, CancellationToken cancellationToken) where T : NetMessageBase;
        Task<T?> TaskMessageAsync<T>(string key, NetMessageBase message, CancellationToken cancellationToken) where T : NetMessageBase;
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

        public async Task SendMessageAsync<T>(T message, CancellationToken cancellationToken) where T : NetMessageBase
        {
            var sendMessage = new NetSendMessage()
            {
                MessageType = message.GetType().Name,
                JsonPayload = _codec.EncodeAsJson(message),
                Destination = message.Destination,
            };

            await _worker.QueueMessageAsync(ERabbitWorkerType.SENDER.ToString(), sendMessage, cancellationToken);
        }

        public async Task<T?> TaskMessageAsync<T>(string key, NetMessageBase message, CancellationToken cancellationToken) where T : NetMessageBase
        {
            var sendMessage = new NetSendMessage()
            {
                MessageType = message.GetType().Name,
                Destination = message.Destination,
                //IsResponsMessage = true,
                TakeId = message.TakeId,
            };

            sendMessage.JsonPayload = _codec.EncodeAsJson(sendMessage);

            return await _worker.TakeMessageAsync<NetSendMessage>(key, sendMessage, cancellationToken) as T;
        }
    }
}
