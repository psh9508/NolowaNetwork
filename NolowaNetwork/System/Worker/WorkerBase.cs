using NolowaNetwork.Models.Message;
using System.Collections.Concurrent;
using System.Threading.Channels;
using static NolowaNetwork.System.Worker.RabbitWorker;

namespace NolowaNetwork.System.Worker
{
    public interface IWorker
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task QueueMessageAsync(string workerType, dynamic message, CancellationToken cancellationToken = default);
        Task HandleReceiveMessageAsync(dynamic message, CancellationToken cancellationToken);
        Task<T?> TakeMessageAsync<T>(string key, dynamic message, CancellationToken cancellationToken = default) where T : NetMessageBase;
        Task TakeBackResponseMessage(string key, dynamic response, CancellationToken cancellationToken = default);
    }

    public abstract class WorkerBase : IWorker
    {
        //private readonly IMessageCodec _codec;
        protected readonly IMessageCodec _codec;

        private ConcurrentDictionary<string, Channel<NetMessageBase>> _channels = new();
        protected ConcurrentDictionary<string, Channel<NetMessageBase>> _outboxMap = new();

        private Task? _process = null;
        private bool _isRunning = false;

        protected WorkerBase(IMessageCodec codec)
        {
            _codec = codec;
        }

        public abstract Task HandleReceiveMessageAsync(dynamic message, CancellationToken cancellationToken);

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

                                await HandleReceiveMessageAsync(data, cancellationToken);
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
        
        public async Task TakeBackResponseMessage(string key, dynamic response, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_outboxMap.TryGetValue(key, out var outbox) && await outbox.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
                {
                    await outbox.Writer.WriteAsync(response, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // log
            }
        }

        public async Task<T?> TakeMessageAsync<T>(string key, dynamic message, CancellationToken cancellationToken = default) where T : NetMessageBase
        {
            try
            {
                var registeredOutbox = RegisterOutboxChannel(key);

                if (registeredOutbox is null)
                    return null;

                await QueueMessageAsync(ERabbitWorkerType.SENDER.ToString(), message, cancellationToken);

                var responseMessage = await registeredOutbox.Reader.ReadAsync(cancellationToken) as T;

                if(responseMessage is null)
                {
                    // log
                    return null;
                }

                return responseMessage;
            }
            catch (Exception ex)
            {
                // log
            }
            finally
            {
                DeregisterOutboxChannel(key);
            }

            return null;
        }

        private Channel<NetMessageBase>? RegisterOutboxChannel(string key)
        {
            var outbox = Channel.CreateBounded<NetMessageBase>(10);

            if(_outboxMap.TryAdd(key, outbox) == false)
            {
                // log
                return null;
            }

            return outbox;
        }

        private void DeregisterOutboxChannel(string key)
        {
            if (_outboxMap.Remove(key, out var channel))
            {
                channel.Writer.Complete();
                return;
            }
        }
    }
}
