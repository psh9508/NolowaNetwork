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
    public class RabbitNetworkClient : INolowaNetwork
    {
        private readonly IMessageCodec _messageCodec;
        private readonly IMessageTypeResolver _messageTypeResolver;
        private readonly IWorker _worker;

        private IConnection _connection;
        private string _exchangeName = string.Empty;
        private string _serverName = string.Empty;

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

            _worker.StartAsync(CancellationToken.None);

            return true;
        }

        public void Send<T>(T message) where T : NetMessageBase
        {
            var channel = _connection.CreateModel();

            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent;

            //var sendMessage = new NetSendMessage()
            //{
            //    MessageType = typeof(T).Name,
            //    Payload = message.,
            //    Destination = message.Destination,
            //};

            var encodeMethod = _messageCodec.GetType().GetMethod("Encode").MakeGenericMethod(message.GetType());

            if (encodeMethod is null)
            {
                return;
            }

            var messagePayload = (byte[]?)encodeMethod.Invoke(_messageCodec, [message]);

            if (messagePayload is null)
            {
                return;
            }

            //var sendMessage = new NetSendMessage()
            //{
            //    MessageType = typeof(T).Name,
            //    Payload = messagePayload,
            //    Destination = message.Destination,
            //};

            //var routingKey = $"{_serverName}.{sendMessage.Destination}.{sendMessage.MessageType}";
            var routingKey = "";

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

                if (decodeMessage is null)
                {
                    // log
                    return;
                }

                var receiveMessage = new NetReceiveMessage(decodeMessage);

                await _worker.QueueMessageAsync(ERabbitWorkerType.RECEIVER.ToString(), receiveMessage, CancellationToken.None);
            }
            catch (Exception ex)
            {
            }
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
