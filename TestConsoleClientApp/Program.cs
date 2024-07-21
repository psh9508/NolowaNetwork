using NolowaNetwork.Models.Configuration;
using NolowaNetwork;
using NolowaNetwork.Models.Message;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;
using Autofac;
using NolowaNetwork.Module;
using static NolowaNetwork.System.Worker.RabbitWorker;

namespace TestConsoleClientApp
{
    public class MessageHandler : IMessageHandler
    {
        public Task HandleAsync(NetMessageBase message, CancellationToken cancellationToken)
        {
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

            var rabbitNetwork = container.Resolve<INolowaNetworkSendable>();
            rabbitNetwork.Connect(new NetworkConfigurationModel()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName",
            });

            var messageBroker = container.Resolve<IMessageBroker>();
            string message = string.Empty;

            do
            {
                Console.WriteLine("Input message: ");

                message = Console.ReadLine();

                var messageModel = new TestMessage();
                messageModel.MessageType = messageModel.GetType().Name;
                messageModel.Message = message;
                messageModel.Destination = "serverName"; // 임시 목적지

                messageBroker.SendMessageAsync(messageModel, CancellationToken.None).ConfigureAwait(false);
            } while (message?.Trim() != "exit");
        }
    }
}
