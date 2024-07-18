using NolowaNetwork.Models.Configuration;
using NolowaNetwork;
using NolowaNetwork.Models.Message;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;

namespace TestConsoleClientApp
{
    class TempMessageResolver : IMessageTypeResolver
    {
        public void AddType(Type type)
        {
            throw new NotImplementedException();
        }

        public Type? GetType(string typeName)
        {
            if (typeName is "TestMessage")
                return typeof(TestMessage);

            return null;
        }

        public dynamic GetTypeByDynamic(string typeName)
        {
            throw new NotImplementedException();
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var rabbitNetwork = new RabbitNetwork(new MessageCodec(), new TempMessageResolver(), new RabbitWorker(null));
            rabbitNetwork.Init(new NetworkConfigurationModel()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName",
            });

            do
            {
                Console.WriteLine("Input message: ");

                var message = Console.ReadLine();

                var messageModel = new TestMessage();
                messageModel.Message = message;
                messageModel.Destination = "serverName"; // 임시 목적지

                rabbitNetwork.Send(messageModel);
            } while (Console.ReadLine()?.Trim() != "exit");
        }
    }
}
