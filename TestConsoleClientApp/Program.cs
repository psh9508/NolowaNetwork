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

            var worker = new RabbitWorker(null);
            var messageBroker = new MessageBroker(worker);

            var rabbitNetwork = new RabbitNetworkClient(new MessageCodec(), new TempMessageResolver(), worker);
            rabbitNetwork.Init(new NetworkConfigurationModel()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName",
            });

            string message = string.Empty;

            do
            {
                Console.WriteLine("Input message: ");

                message = Console.ReadLine();

                var messageModel = new TestMessage();
                messageModel.Message = message;
                messageModel.Destination = "serverName"; // 임시 목적지

                messageBroker.SendMessageAsync(messageModel, CancellationToken.None).ConfigureAwait(false);
            } while (message?.Trim() != "exit");
        }
    }
}
