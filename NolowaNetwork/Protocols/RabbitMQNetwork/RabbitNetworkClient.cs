using NolowaNetwork.Models.Configuration;
using NolowaNetwork.System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NolowaNetwork.System.Worker;
using static NolowaNetwork.System.Worker.RabbitWorker;
using NolowaNetwork.Models.Message;
using Polly.Retry;
using Polly;
using System.Net.Sockets;
using RabbitMQ.Client.Exceptions;
using Serilog;

namespace NolowaNetwork.Protocols.RabbitMQNetwork
{
    public partial class RabbitNetworkClient : IMessageQueue
    {
        private const int RETRY_COUNT = 4;
        private readonly Random _random = new Random();
        private RetryPolicy _retryPolicy;

        private readonly ILogger _logger;
        private readonly IMessageCodec _messageCodec;
        private readonly IMessageTypeResolver _messageTypeResolver;
        private readonly Lazy<IWorker> _worker; // 데이터를 받는 곳에서만 필요하다. 그때만 실제 객체를 생성해서 순환참조를 막는다.

        private IConnection? _connection;
        private string _exchangeName = string.Empty;
        private string _serverName = string.Empty;

        public RabbitNetworkClient(IMessageCodec messageCodec, IMessageTypeResolver messageTypeResolver, Lazy<IWorker> worker, ILogger logger)
        {
            _messageCodec = messageCodec;
            _messageTypeResolver = messageTypeResolver;
            _worker = worker;
            _retryPolicy = Policy.Handle<SocketException>().Or<BrokerUnreachableException>()
                                .WaitAndRetry(RETRY_COUNT, retryAttempt => TimeSpan.FromSeconds(retryAttempt * _random.Next(1 * retryAttempt, 2 * retryAttempt)), (exception, timespan) =>
                                {
                                    //timespan.TotalSeconds,
                                    //exception.Message
                                });
            _logger = logger;
        }

        public bool Connect(NetworkConfigurationModel configuration)
        {
            if (_connection?.IsOpen == true)
                return true;

            var setting = configuration;

            if (VerifySetting(setting) == false)
                return false;

            var factory = new ConnectionFactory
            {
                HostName = setting.Address,
                VirtualHost = setting.HostName,
                Port = 5672,
                UserName = "admin",
                Password = "admin",
                //Port = 30501,
                //UserName = "owadmin",
                //Password = "owrhksflwk123!",
                DispatchConsumersAsync = true,
            };

            try
            {
                _exchangeName = setting.ExchangeName;
                _serverName = setting.ServerName;

                _retryPolicy.Execute(() =>
                {
                    _connection = factory.CreateConnection();
                });

                if (_connection is null || _connection.IsOpen == false)
                {
                    _logger.Error("Connection is null or not opened");
                    return false;
                }

                var channel = _connection.CreateModel();

                channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Topic);
                channel.QueueDeclare(queue: _serverName, durable: false, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                        ["x-single-active-consumer"] = true,
                        ["x-expires"] = 300000,
                    });
                channel.QueueBind(queue: _serverName
                                , exchange: _exchangeName
                                , routingKey: $@"*.{_serverName}.*");

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += OnConsumerReceived;

                channel.BasicConsume(queue: _serverName, autoAck: true, consumer);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Connection is failed! {HostName} {VirtualHost} {Port} {UserName}", factory.HostName, factory.VirtualHost, factory.Port, factory.UserName);
                throw;
            }
        }

        public void Send<T>(T message) where T : NetMessageBase
        {
            var routingKey = $"{_serverName}.{message.Destination}.{nameof(NetSendMessage)}";

            try
            {
                var channel = _connection.CreateModel();

                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent;

                var messagePayload = _messageCodec.EncodeAsByte(message);

                _retryPolicy.Execute(() =>
                {
                    channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: messagePayload
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Send failed! {routingKey} {@message}", routingKey, message);
            }
        }

        private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                var (sourceServer, targetServer, messageName) = ParseMessage(eventArgs.RoutingKey);

                var messageType = _messageTypeResolver.GetType(messageName);

                if (messageType == null)
                {
                    _logger.Error("messageType is null {messageName}", messageName);
                    return;
                }

                var decodeMethod = _messageCodec.GetType().GetMethod("Decode")!.MakeGenericMethod(messageType);

                if (decodeMethod is null)
                {
                    _logger.Error("decodeMethod is null");
                    return;
                }

                dynamic? decodeMessage = decodeMethod.Invoke(_messageCodec, [eventArgs.Body]);

                if (decodeMessage is null)
                {
                    _logger.Error("decodeMessage is null");
                    return;
                }

                await ReceiveAsync(decodeMessage);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.ToString());
            }
        }

        public async Task ReceiveAsync(NetMessageBase message)
        {
            var receiveMessage = new NetReceiveMessage(message);

            await _worker.Value.QueueMessageAsync(ERabbitWorkerType.RECEIVER.ToString(), receiveMessage, CancellationToken.None);
        }

        private (string sourceServer, string targetServer, string messageName) ParseMessage(string routingKey)
        {
            var parsedKey = routingKey.Split('.');

            return (sourceServer: parsedKey[0]
                  , targetServer: parsedKey[1]
                  , messageName: parsedKey[2]);
        }

        private bool VerifySetting(NetworkConfigurationModel model)
        {
            if (model is null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(model.HostName))
            {
                return false;
            }

            return true;
        }

    }
}
