using NolowaNetwork.Models.Configuration;
using NolowaNetwork.System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;

namespace NolowaNetwork
{
    public class RabbitNetwork : INolowaNetwork
    {
        private readonly IMessageCodec _messageCodec;
        private readonly IMessageTypeResolver _messageTypeResolver;

        public RabbitNetwork(IMessageCodec messageCodec, IMessageTypeResolver messageTypeResolver)
        {
            _messageCodec = messageCodec;
            _messageTypeResolver = messageTypeResolver;
        }

        public bool Init(NetworkConfigurationModel configuration)
        {
            var setting = configuration;

            if (VerifySetting(setting) == false)
                return false;

            var factory = new ConnectionFactory
            {
                HostName = setting.HostName,
                VirtualHost = "/",
                Port = 5672,
                UserName = "admin",
                Password = "admin",
                DispatchConsumersAsync = true,
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: setting.ExchangeName, type: ExchangeType.Topic);
            channel.QueueDeclare(queue: setting.ServerName, durable: false, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-single-active-consumer"] = true,
                    ["x-expires"] = 300000,
                });
            channel.QueueBind(queue: setting.ServerName
                            , exchange: setting.ExchangeName
                            , routingKey: $@"*.{setting.ServerName}.*");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += OnConsumerReceived;

            channel.BasicConsume(queue: setting.ServerName, autoAck: true, consumer);

            return true;
        }

        private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs eventArgs)
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

            if(decodeMessage is null)
            {
                // log
                return;
            }



            await Task.Yield();
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
