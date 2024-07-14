using NolowaNetwork.Models.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NolowaNetwork.System.Worker
{
    internal abstract class WorkerBase
    {
        private Channel<INetMessage> _channel;

        private Task? _process = null;
        private bool _isRunning = false;

        protected abstract Task ReceiveAsync(INetMessage message, CancellationToken cancellationToken);

        public WorkerBase()
        {
            _channel = Channel.CreateBounded<INetMessage>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;

            _process = Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        if (await _channel.Reader.WaitToReadAsync(cancellationToken) == false)
                        {
                            // 현재 channel에 데이터를 읽을 수 없음.
                            continue;
                        }

                        dynamic data = await _channel.Reader.ReadAsync(cancellationToken);

                        await ReceiveAsync(data, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // log
                        throw;
                    }
                }
            });
        }

        public async Task QueueMessageAsync(dynamic message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _channel.Writer.WaitToWriteAsync(cancellationToken))
                {
                    await _channel.Writer.WriteAsync(message, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // log
            }
        }
    }
}
