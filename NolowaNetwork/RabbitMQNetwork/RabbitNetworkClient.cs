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

namespace NolowaNetwork.RabbitMQNetwork
{
    public partial class RabbitNetworkClient : INolowaNetworkSendable, INolowaNetworkReceivable
    {
        private readonly IMessageCodec _messageCodec;
        private readonly IMessageTypeResolver _messageTypeResolver;

        private IConnection? _connection;
        private string _exchangeName = string.Empty;
        private string _serverName = string.Empty;
       

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
                    HostName = setting.HostName,
                    VirtualHost = "/",
                    Port = 5672,
                    UserName = "admin",
                    Password = "admin",
                    //Port = 30501,
                    //UserName = "owadmin",
                    //Password = "owrhksflwk123!",
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
            catch (Exception ex)
            {
                // log
                throw;
            }
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
