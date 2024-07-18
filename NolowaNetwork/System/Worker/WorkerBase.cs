using NolowaNetwork.Models.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static NolowaNetwork.System.Worker.RabbitWorker;

namespace NolowaNetwork.System.Worker
{
    public interface IWorker
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task QueueMessageAsync(string key, dynamic message, CancellationToken cancellationToken = default);
        Task ReceiveAsync(dynamic message, CancellationToken cancellationToken);
    }

    public abstract class WorkerBase : IWorker
    {
        private ConcurrentDictionary<string, Channel<NetMessageBase>> _channels = new();

        private Task? _process = null;
        private bool _isRunning = false;

        public abstract Task ReceiveAsync(dynamic message, CancellationToken cancellationToken);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;

            await Parallel.ForEachAsync(_channels, async (channel, cancellationToken) =>
            {
                _ = Task.Run(async () =>
                {
                    while (_isRunning)
                    {
                        try
                        {
                            if (await channel.Value.Reader.WaitToReadAsync(cancellationToken))
                            {
                                dynamic data = await channel.Value.Reader.ReadAsync(cancellationToken);

                                await ReceiveAsync(data, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            // log
                            // this thread never die.
                        }
                    }
                });
            });
        }

        public bool AddChannel(string key, Channel<NetMessageBase> channel)
        {
            if (_channels.ContainsKey(key) == true)
                return false;

            _channels[key] = channel;
            
            return true;
        }

        public Channel<NetMessageBase>? GetChannel(string key)
        {
            return _channels.ContainsKey(key) ? _channels[key] : null;
        }

        public async Task QueueMessageAsync(string key, dynamic message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_channels.ContainsKey(key) == false)
                    throw new InvalidOperationException("");

                if (await _channels[key].Writer.WaitToWriteAsync(cancellationToken))
                {
                    await _channels[key].Writer.WriteAsync(message, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // log
            }
        }
    }
}
