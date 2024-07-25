using Autofac;
using NolowaNetwork;
using NolowaNetwork.Models.Configuration;
using NolowaNetwork.Models.Message;
using NolowaNetwork.Module;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;
using System.Text.Json;
using static NolowaNetwork.System.Worker.RabbitWorker;

namespace TestConsoleApp
{
    public class MessageHandler : IMessageHandler
    {
        public Task HandleAsync(NetMessageBase message, CancellationToken cancellationToken)
        {
            string messageType = message.MessageType;
            string jsonPayload = message.JsonPayload;

            var test = JsonSerializer.Deserialize<TestMessage>(jsonPayload);

            // 다시 받은 서버로 보내주는 로직 추가
            // 이때 메시지의 IsResponsMessage = true

            return Task.CompletedTask;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<MessageHandler>().As<IMessageHandler>();

            new RabbitMQModule().RegisterModule(containerBuilder);

            var container = containerBuilder.Build();

            var rabbitNetwork = container.Resolve<INolowaNetworkReceivable>();
            rabbitNetwork.Connect(new NetworkConfigurationModel()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName",
            });

            Console.ReadKey();
        }
    }
}
