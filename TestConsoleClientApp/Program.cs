using NolowaNetwork.Models.Configuration;
using NolowaNetwork;
using NolowaNetwork.Models.Message;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;
using Autofac;
using NolowaNetwork.Module;

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
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<MessageHandler>().As<IMessageHandler>();

            new RabbitMQModule().RegisterModule(containerBuilder);

            var container = containerBuilder.Build();

            //var rabbitNetwork = container.Resolve<INolowaNetworkSendable>();
            //rabbitNetwork.Connect(new NetworkConfigurationModel()
            //{
            //    HostName = "localhost",
            //    ExchangeName = "exchangeName",
            //    ServerName = "serverName:2:sender",
            //});

            //var rabbitReceiver = container.Resolve<INolowaNetworkReceivable>();
            //rabbitReceiver.Connect(new NetworkConfigurationModel()
            //{
            //    HostName = "localhost",
            //    ExchangeName = "exchangeName",
            //    ServerName = "serverName:2",
            //});

            var rabbitNetwork = container.Resolve<INolowaNetworkClient>();
            rabbitNetwork.Connect(new NetworkConfigurationModel()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName:2",
            });

            var messageBroker = container.Resolve<IMessageBroker>();
            var messageCodec = container.Resolve<IMessageCodec>();

            await StartTakeMessageTest(messageBroker, messageCodec);
            //StartSendMessageTest(messageBroker);
        }

        private static void StartSendMessageTest(IMessageBroker messageBroker)
        {
            string message = string.Empty;

            do
            {
                Console.WriteLine("Input message: ");

                message = Console.ReadLine();

                var messageModel = new TestMessage();
                messageModel.MessageType = messageModel.GetType().Name;
                messageModel.Message = message;
                messageModel.Destination = "serverName:1"; // 임시 목적지

                messageBroker.SendMessageAsync(messageModel, CancellationToken.None).ConfigureAwait(false);
            } while (message?.Trim() != "exit");
        }

        private static async Task StartTakeMessageTest(IMessageBroker messageBroker, IMessageCodec messageCodec)
        {
            string message = string.Empty;

            do
            {
                Console.WriteLine("Input message: ");

                message = Console.ReadLine();

                var messageModel = new TestMessage();
                messageModel.TakeId = Guid.NewGuid().ToString();
                messageModel.MessageType = messageModel.GetType().Name;
                messageModel.Message = message;
                messageModel.Origin = "serverName:2";
                messageModel.Source = "serverName:2";
                messageModel.Destination = "serverName:1";

                messageModel.JsonPayload = messageCodec.EncodeAsJson(messageModel);

                var response = await messageBroker.TaskMessageAsync<ResponseMessage>(messageModel.TakeId, messageModel, CancellationToken.None);

                if(response is null)
                {
                    Console.WriteLine("response is null");
                    continue;
                }

                Console.WriteLine($"I get a message : {response.Message}");
            } while(message?.Trim() != "exit");
        }

    }
}
