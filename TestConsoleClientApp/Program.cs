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

            var rabbitNetwork = container.Resolve<INolowaNetworkClient>();
            rabbitNetwork.Connect(new NetworkConfigurationModel()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName:2",
            });

            var messageBroker = container.Resolve<IMessageBroker>();
            var messageCodec = container.Resolve<IMessageCodec>();
            var messageMaker = container.Resolve<IMessageMaker>();

            await StartTakeMessageTest(messageBroker, messageCodec, messageMaker);
            //StartSendMessageTest(messageBroker, messageMaker);
        }

        private static void StartSendMessageTest(IMessageBroker messageBroker, IMessageMaker messageMaker)
        {
            string message = string.Empty;

            do
            {
                Console.WriteLine("Input message: ");

                message = Console.ReadLine();

                var sendMessage = messageMaker.MakeStartMessage<TestMessage>("serverName:2","serverName:1");
                sendMessage.Message = message;

                messageBroker.SendMessageAsync(sendMessage, CancellationToken.None).ConfigureAwait(false);
            } while (message?.Trim() != "exit");
        }

        private static async Task StartTakeMessageTest(IMessageBroker messageBroker, IMessageCodec messageCodec, IMessageMaker messageMaker)
        {
            string message = string.Empty;

            do
            {
                Console.WriteLine("Input message: ");

                message = Console.ReadLine();

                var messageModel = messageMaker.MakeTakeMessage<TestMessage>("serverName:2", "serverName:1");
                messageModel.Message = message;

                //var messageModel = new TestMessage();
                //messageModel.TakeId = Guid.NewGuid().ToString();
                //messageModel.MessageType = messageModel.GetType().Name;
                //messageModel.Message = message;
                //messageModel.Origin = "serverName:2";
                //messageModel.Source = "serverName:2";
                //messageModel.Destination = "serverName:1";

                messageModel.JsonPayload = messageCodec.EncodeAsJson(messageModel);

                var response = await messageBroker.TakeMessageAsync<ResponseMessage>(messageModel.TakeId, messageModel, CancellationToken.None);

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
