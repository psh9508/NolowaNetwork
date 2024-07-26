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
        private readonly ILifetimeScope _scope;

        public MessageHandler(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task HandleAsync(NetMessageBase message, CancellationToken cancellationToken)
        {
            string messageType = message.MessageType;
            string jsonPayload = message.JsonPayload;

            var receivedMessage = JsonSerializer.Deserialize<TestMessage>(jsonPayload);

            using var scope = _scope.BeginLifetimeScope();

            var job = scope.Resolve<IJob>();

            // 받은 메시지를 이용해 이 서버에서 뭔가 진행했다고 가정
            await job.RunAsync(receivedMessage);
        }
    }

    public interface IJob
    {
        Task RunAsync(TestMessage receivedMessage);
    }

    public class Job : IJob
    {
        private readonly IMessageBroker _messageBroker;

        public Job(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public async Task RunAsync(TestMessage receivedMessage)
        {
            await Task.Delay(1000);
            Console.WriteLine($"I got a message : {receivedMessage.Message}");

            // 다시 받은 서버로 보내주는 로직 추가
            // 이때 메시지의 IsResponsMessage = true
            var sendMessage = new TestMessage()
            {
                TakeId = receivedMessage.TakeId,
                MessageType = receivedMessage.GetType().Name,
                Message = "잘 도착해서 처리 완료 하고 돌려드립니다.",
                Origin = receivedMessage.Origin,
                Source = "serverName:1",
                Destination = receivedMessage.Origin, // 받은곳으로 돌려줌
                IsResponsMessage = true,
            };

            await _messageBroker.SendMessageAsync(sendMessage, CancellationToken.None);
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
                ServerName = "serverName:1",
            });

            var rabbitNetwork = container.Resolve<INolowaNetworkSendable>();
            rabbitNetwork.Connect(new ()
            {
                HostName = "localhost",
                ExchangeName = "exchangeName",
                ServerName = "serverName:1:sender",
            });

            Console.ReadKey();
        }
    }
}
