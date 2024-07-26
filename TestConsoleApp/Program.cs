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
        private readonly IJob _job;

        public MessageHandler(IJob job)
        {
            _job = job;
        }

        public async Task HandleAsync(NetMessageBase message, CancellationToken cancellationToken)
        {
            string messageType = message.MessageType;
            string jsonPayload = message.JsonPayload;

            var receivedMessage = JsonSerializer.Deserialize<TestMessage>(jsonPayload);

            // 받은 메시지를 이용해 이 서버에서 뭔가 진행했다고 가정
            

            // 다시 받은 서버로 보내주는 로직 추가
            // 이때 메시지의 IsResponsMessage = true
            var sendMessage = new TestMessage()
            {
                TakeId = receivedMessage.TakeId,
                MessageType = messageType,
                Message = "잘 도착해서 처리 완료 하고 돌려드립니다.",
                Origin = receivedMessage.Origin,
                Source = "serverName:1",
                Destination = receivedMessage.Origin, // 받은곳으로 돌려줌
                IsResponsMessage = true,
            };
        }
    }

    public interface IJob
    {
        Task Run(TestMessage receivedMessage);
    }

    public class Job : IJob
    {
        private readonly IMessageBroker _messageBroker;

        public Job(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public async Task Run(TestMessage receivedMessage)
        {
            await Task.Delay(1000);
            Console.WriteLine($"I got a message : {receivedMessage.Message}");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<MessageHandler>().As<IMessageHandler>();
            containerBuilder.RegisterType<Job>().As<IJob>();

            new RabbitMQModule().RegisterModule(containerBuilder);

            var container = containerBuilder.Build();

            var rabbitSender = container.Resolve<INolowaNetworkReceivable>();
            rabbitSender.Connect(new()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName:1:sender",
            });

            var rabbitNetwork = container.Resolve<INolowaNetworkReceivable>();
            rabbitNetwork.Connect(new NetworkConfigurationModel()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName:1",
            });

            Console.ReadKey();
        }
    }
}
