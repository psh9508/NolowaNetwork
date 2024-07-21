using NolowaNetwork.Models.Message;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NolowaNetwork.System.Worker.RabbitWorker;

namespace NolowaNetwork.RabbitMQNetwork
{
    /// <summary>
    /// RabbitNetworkReceiveClient
    /// RabbitNetworkClient의 partial 클래스
    /// </summary>
    public partial class RabbitNetworkClient
    {
        private readonly IWorker _worker;

        // receive용 worker가 필요
        public RabbitNetworkClient(IMessageCodec messageCodec, IMessageTypeResolver messageTypeResolver, IWorker worker)
        {
            _messageCodec = messageCodec;
            _messageTypeResolver = messageTypeResolver;
            _worker = worker;
        }

        private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                var (sourceServer, targetServer, messageName) = ParseMessage(eventArgs.RoutingKey);

                var messageType = _messageTypeResolver.GetType(messageName);

                if (messageType == null)
                {
                    // log
                    return;
                }

                var decodeMethod = _messageCodec.GetType().GetMethod("Decode")!.MakeGenericMethod(messageType);

                if (decodeMethod is null)
                {
                    // log
                    return;
                }

                dynamic? decodeMessage = decodeMethod.Invoke(_messageCodec, [eventArgs.Body]);

                if (decodeMessage is null)
                {
                    // log
                    return;
                }

                await ReceiveAsync(decodeMessage);
            }
            catch (Exception ex)
            {
                // log
            }
        }

        public async Task ReceiveAsync(NetMessageBase message)
        {
            var receiveMessage = new NetReceiveMessage(message);

            await _worker.QueueMessageAsync(ERabbitWorkerType.RECEIVER.ToString(), receiveMessage, CancellationToken.None);
        }

        private (string sourceServer, string targetServer, string messageName) ParseMessage(string routingKey)
        {
            var parsedKey = routingKey.Split('.');

            return (sourceServer: parsedKey[0]
                  , targetServer: parsedKey[1]
                  , messageName: parsedKey[2]);
        }
    }
}
