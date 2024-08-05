using NolowaNetwork.Models.Configuration;
using NolowaNetwork.System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using NolowaNetwork.System.Worker;
using static NolowaNetwork.System.Worker.RabbitWorker;
using NolowaNetwork.Models.Message;
using System.Reflection;
using System.Data.Common;
using Polly.Retry;
using Polly;
using System.Net.Sockets;
using RabbitMQ.Client.Exceptions;

namespace NolowaNetwork.RabbitMQNetwork
{
    public partial class RabbitNetworkClient : INolowaNetworkClient
    {
        private const int RETRY_COUNT = 4;
        private readonly Random _random = new Random();

        private readonly IMessageCodec _messageCodec;
        private readonly IMessageTypeResolver _messageTypeResolver;
        private readonly Lazy<IWorker> _worker; // 데이터를 받는 곳에서만 필요하다. 그때만 실제 객체를 생성해서 순환참조를 막는다.

        private IConnection? _connection;
        private string _exchangeName = string.Empty;
        private string _serverName = string.Empty;

        public RabbitNetworkClient(IMessageCodec messageCodec, IMessageTypeResolver messageTypeResolver, Lazy<IWorker> worker)
        {
            _messageCodec = messageCodec;
            _messageTypeResolver = messageTypeResolver;
            _worker = worker;
        }

        public bool Connect(NetworkConfigurationModel configuration)
        {
            try
            {
                if (_connection?.IsOpen == true)
                    return true;

                var setting = configuration;

                if (VerifySetting(setting) == false)
                    return false;

                _exchangeName = setting.ExchangeName;
                _serverName = setting.ServerName;

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

                var retryPolicy = Policy.Handle<SocketException>().Or<BrokerUnreachableException>()
                                        .WaitAndRetry(RETRY_COUNT, retryAttempt => TimeSpan.FromSeconds(retryAttempt * _random.Next(1 * retryAttempt, 2 * retryAttempt)), (exception, timespan) =>
                                        {
                                            //timespan.TotalSeconds,
                                            //exception.Message
                                        });

                retryPolicy.Execute(() =>
                {
                    _connection = factory.CreateConnection();
                });

                if(_connection is null || _connection.IsOpen == false)
                {
                    // log
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
                // log
                throw;
            }
        }

        public void Send<T>(T message) where T : NetMessageBase
        {
            try
            {
                var channel = _connection.CreateModel();

                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent;

                var messagePayload = _messageCodec.EncodeAsByte(message);

                var routingKey = $"{_serverName}.{message.Destination}.{nameof(NetSendMessage)}";

                var retryPolicy = Policy.Handle<SocketException>().Or<BrokerUnreachableException>()
                                        .WaitAndRetry(RETRY_COUNT, retryAttempt => TimeSpan.FromSeconds(retryAttempt * _random.Next(1 * retryAttempt, 2 * retryAttempt)), (exception, timespan) =>
                                        {
                                            //timespan.TotalSeconds,
                                            //exception.Message
                                        });
                retryPolicy.Execute(() =>
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
                // log
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
