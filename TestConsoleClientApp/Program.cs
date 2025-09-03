using NolowaNetwork.Models.Configuration;
using NolowaNetwork;
using NolowaNetwork.Models.Message;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;
using Autofac;
using NolowaNetwork.Module;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace TestConsoleClientApp
{
    public class TypeResolver : IMessageTypeResolver
    {
        public void AddType(Type type)
        {
            throw new NotImplementedException();
        }

        public Type? GetType(string typeName)
        {
            if (typeName is "TestMessage")
                return typeof(TestMessage);
            else if (typeName is "ResponseMessage")
                return typeof(ResponseMessage);

            return null;
        }

        public dynamic GetTypeByDynamic(string typeName)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageHandler : IMessageHandler
    {
        public Task HandleAsync(dynamic message, CancellationToken cancellationToken)
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
            containerBuilder.RegisterType<TypeResolver>().As<IMessageTypeResolver>();
            containerBuilder.RegisterType<MessageHandler>().As<IMessageHandler>();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            containerBuilder.RegisterInstance(Log.Logger);

            new RabbitMQModule().RegisterModule(containerBuilder);
            new RabbitMQModule().SetConfiguration(containerBuilder);

            var container = containerBuilder.Build();

            var messageBroker = container.Resolve<IMessageBroker>();
            var messageCodec = container.Resolve<IMessageCodec>();
            var messageMaker = container.Resolve<IMessageMaker>();
            var configuration = container.Resolve<IConfiguration>();

            var serverSettingModel = configuration.GetSection("Network").GetSection("RabbitMQ").Get<NetworkConfigurationModel>();

            var rabbitNetwork = container.Resolve<IMessageBus>();
            bool connectResult = rabbitNetwork.Connect(serverSettingModel);

            if(connectResult == false)
            {
                throw new Exception("message queue 연결 실패");
            }

            await StartTakeMessageTest(messageBroker, messageCodec, messageMaker, serverSettingModel);
            //StartSendMessageTest(messageBroker, messageMaker);
        }

        private static void StartSendMessageTest(IMessageBroker messageBroker, IMessageMaker messageMaker, NetworkConfigurationModel serverSettingModel)
        {
            string message = string.Empty;

            do
            {
                Console.WriteLine("Input message: ");

                message = Console.ReadLine();

                var sendMessage = messageMaker.MakeStartMessage<TestMessage>(serverSettingModel.ServerName, "serverName:1");
                sendMessage.Message = message;

                messageBroker.SendMessageAsync(sendMessage, CancellationToken.None).ConfigureAwait(false);
            } while (message?.Trim() != "exit");
        }

        private static async Task StartTakeMessageTest(IMessageBroker messageBroker, IMessageCodec messageCodec, IMessageMaker messageMaker, NetworkConfigurationModel serverSettingModel)
        {   
            string message = string.Empty;

            do
            {
                Console.WriteLine("Input message: ");

                message = Console.ReadLine();

                var messageModel = messageMaker.MakeTakeMessage<TestMessage>(serverSettingModel.ServerName, "server:1");
                messageModel.Message = message;

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
