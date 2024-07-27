using NolowaNetwork.Models.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NolowaNetwork.System.Worker
{
    public class RabbitWorker : WorkerBase
    {
        public enum ERabbitWorkerType
        {
            RECEIVER,
            SENDER,
        }

        // 서버단에서 구현되어 처리 된다.
        private readonly IMessageHandler _messageHandler;
        private readonly INolowaNetworkSendable _nolowaNetworkSender;
        

        public RabbitWorker(IMessageHandler messageHandler, INolowaNetworkSendable nolowaNetworkSendable, IMessageCodec messageCodec) : base(messageCodec)
        {
            _messageHandler = messageHandler;
            _nolowaNetworkSender = nolowaNetworkSendable;

            AddChannel(ERabbitWorkerType.RECEIVER.ToString(), Channel.CreateBounded<NetMessageBase>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            }));
            AddChannel(ERabbitWorkerType.SENDER.ToString(), Channel.CreateBounded<NetMessageBase>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            }));
        }

        public override async Task HandleReceiveMessageAsync(dynamic message, CancellationToken cancellationToken)
        {
            await ReceiveInternalAsync(message, cancellationToken).ConfigureAwait(false);
        }

        // 보낼 메시지 큐
        private async Task ReceiveInternalAsync(NetSendMessage message, CancellationToken cancellationToken)
        {
            _nolowaNetworkSender.Send(message);
        }

        // 받는 메시지 큐
        private async Task ReceiveInternalAsync(NetReceiveMessage message, CancellationToken cancellationToken)
        {
            if (message.IsResponsMessage)
            {
                await TakeBackResponseMessage(message.TakeId, message, cancellationToken);
            }
            else
            {
                if (_messageHandler is null)
                    throw new InvalidOperationException("IMessageHandler is null. It must be registered before you start.");

                await _messageHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
