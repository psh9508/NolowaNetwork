using NolowaNetwork.Models.Message;
using System;
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
        private IMessageHandler _messageHandler;

        public RabbitWorker(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;

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

        public override async Task ReceiveAsync(dynamic message, CancellationToken cancellationToken)
        {
            await ReceiveInternalAsync(message, cancellationToken).ConfigureAwait(false);
        }

        // 보낼 메시지 큐
        private async Task ReceiveInternalAsync(NetSendMessage message, CancellationToken cancellationToken)
        {

        }

        // 받는 메시지 큐
        private async Task ReceiveInternalAsync(NetReceiveMessage message, CancellationToken cancellationToken)
        {
            await _messageHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
