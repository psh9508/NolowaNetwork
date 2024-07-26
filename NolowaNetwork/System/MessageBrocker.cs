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
                TakeId = message.TakeId,
                MessageType = message.GetType().Name,
                Origin = message.Origin,
                Source = message.Source,
                Destination = message.Destination,
                IsResponsMessage = message.IsResponsMessage,
            };

            sendMessage.JsonPayload = _codec.EncodeAsJson(sendMessage);

            await _worker.QueueMessageAsync(ERabbitWorkerType.SENDER.ToString(), sendMessage, cancellationToken);
        }

        public async Task<T?> TaskMessageAsync<T>(string key, NetMessageBase message, CancellationToken cancellationToken) where T : NetMessageBase
        {
            var sendMessage = new NetSendMessage()
            {
                TakeId = message.TakeId,
                MessageType = message.GetType().Name,
                Origin = message.Origin,
                Source = message.Source,
                Destination = message.Destination,
                //IsResponsMessage = true,
            };

            sendMessage.JsonPayload = _codec.EncodeAsJson(sendMessage);

            return await _worker.TakeMessageAsync<NetSendMessage>(key, sendMessage, cancellationToken) as T;
        }
    }
}
