using Autofac;
using Microsoft.Extensions.Configuration;
using NolowaNetwork;
using NolowaNetwork.Models.Configuration;
using NolowaNetwork.Models.Message;
using NolowaNetwork.Module;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;
using Serilog;
using System.Text.Json;

namespace TestConsoleApp
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
        private readonly ILifetimeScope _scope;

        public MessageHandler(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task HandleAsync(dynamic message, CancellationToken cancellationToken)
        {
            await HandleMessageAsync(message, cancellationToken);
        }

        public async Task HandleMessageAsync(TestMessage message, CancellationToken cancellationToken)
        {
            using var scope = _scope.BeginLifetimeScope();

            var job = scope.Resolve<IJob>();

            await job.RunAsync(message);
        }
    }

    public interface IJob
    {
        Task RunAsync(TestMessage receivedMessage);
    }

    public class Job : IJob
    {
        private readonly IMessageBroker _messageBroker;
        private readonly IMessageMaker _messageMaker;

        public Job(IMessageBroker messageBroker, IMessageMaker messageMaker)
        {
            _messageBroker = messageBroker;
            _messageMaker = messageMaker;
        }

        public async Task RunAsync(TestMessage receivedMessage)
        {
            // 받은 메시지를 이용해 이 서버에서 뭔가 진행했다고 가정
            await Task.Delay(1000);
            Console.WriteLine($"I got a message : {receivedMessage.Message}");

            var sendMessage = _messageMaker.MakeResponseMessage<ResponseMessage>("server:1", receivedMessage);
            sendMessage.Message = $"당신에게 받은 메시지는 {receivedMessage.Message} 입니다. 잘 도착해서 처리 완료 하고 돌려드립니다.";

            await _messageBroker.SendMessageAsync(sendMessage, CancellationToken.None);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<TypeResolver>().As<IMessageTypeResolver>();
            containerBuilder.RegisterType<MessageHandler>().As<IMessageHandler>();
            containerBuilder.RegisterType<Job>().As<IJob>();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger(); 

            containerBuilder.RegisterInstance(Log.Logger);

            new RabbitMQModule().RegisterModule(containerBuilder);
            new RabbitMQModule().SetConfiguration(containerBuilder);

            var container = containerBuilder.Build();

            var configuration = container.Resolve<IConfiguration>();
            var serverSettingModel = configuration.GetSection("Network").GetSection("RabbitMQ").Get<NetworkConfigurationModel>();

            var rabbitClient = container.Resolve<IMessageBus>();
            rabbitClient.Connect(serverSettingModel);

            Console.ReadKey();
        }
    }
}
