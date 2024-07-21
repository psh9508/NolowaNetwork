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

namespace NolowaNetwork
{
    public class RabbitNetworkClient : INolowaNetworkSendable, INolowaNetworkReceivable
    {
        private readonly IMessageCodec _messageCodec;
        private readonly IMessageTypeResolver _messageTypeResolver;
        private readonly IWorker _worker;

        private IConnection _connection;
        private string _exchangeName = string.Empty;
        private string _serverName = string.Empty;

        // sender용 worker가 필요 없음
        public RabbitNetworkClient(IMessageCodec messageCodec, IMessageTypeResolver messageTypeResolver)
        {
            _messageCodec = messageCodec;
            _messageTypeResolver = messageTypeResolver;
        }

        // receive용 worker가 필요
        public RabbitNetworkClient(IMessageCodec messageCodec, IMessageTypeResolver messageTypeResolver, IWorker worker)
        {
            _messageCodec = messageCodec;
            _messageTypeResolver = messageTypeResolver;
            _worker = worker;
        }

        public bool Init(NetworkConfigurationModel configuration)
        {
            var setting = configuration;

            if (VerifySetting(setting) == false)
                return false;

            _exchangeName = setting.ExchangeName;
            _serverName = setting.ServerName;

            var factory = new ConnectionFactory
            {
                HostName = setting.HostName,
                VirtualHost = "/",
                Port = 5672,
                UserName = "admin",
                Password = "admin",
                DispatchConsumersAsync = true,
            };

            _connection = factory.CreateConnection();
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

        public void Send<T>(T message) where T : NetMessageBase
        {
            var channel = _connection.CreateModel();

            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent;

            var sendMessage = new NetSendMessage()
            {
                MessageType = message.MessageType,
                JsonPayload = message.JsonPayload,
                Destination = message.Destination,
            };

            var messagePayload = _messageCodec.EncodeAsByte(sendMessage);

            var routingKey = $"{_serverName}.{sendMessage.Destination}.{nameof(NetSendMessage)}";

            channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: messagePayload
            );
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

                dynamic? decodeMessage = decodeMethod.Invoke(_messageCodec, [ eventArgs.Body ]);

                var test = decodeMessage as NetSendMessage;

                var test1 = test.JsonPayload.GetType();

                var test2 = test.JsonPayload;

                if (decodeMessage is null)
                {
                    // log
                    return;
                }

                await ReceiveAsync(decodeMessage);
            }
            catch (Exception ex)
            {
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
