using Autofac;
using Microsoft.Extensions.Configuration;
using NolowaNetwork.Protocols.Http;
using NolowaNetwork.Protocols.RabbitMQNetwork;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;

namespace NolowaNetwork.Module
{
    public class RabbitMQModule
    {
        public void RegisterModule(ContainerBuilder builder)
        {
            builder.RegisterType<RabbitWorker>().As<IWorker>()
                .OnActivated(async x => await x.Instance.StartAsync(CancellationToken.None))
                .SingleInstance();

            builder.RegisterType<RabbitNetworkClient>().As<IMessageBus>().SingleInstance();
            builder.RegisterType<HttpProvider>().As<IHttpProvider>().SingleInstance();
            builder.RegisterType<MessageMaker>().As<IMessageMaker>();
            builder.RegisterType<MessageCodec>().As<IMessageCodec>();
            builder.RegisterType<MessageBroker>().As<IMessageBroker>().InstancePerLifetimeScope();
            //builder.RegisterType<MessageTypeResolver>().As<IMessageTypeResolver>(); // 각 서버에서 등록해야한다.
            //builder.RegisterType<MessageHandler>().As<IMessageHandler>();           // 각 서버에서 등록해야한다.

            SetConfiguration(builder);
        }

        public void SetConfiguration(ContainerBuilder builder)
        {
            // 프로젝트 특성상 프로젝트 bin 폴더가 아닌 각각의 실행폴더에서 setting 가져올 수 있도록 함
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

            var environment = Environment.GetEnvironmentVariable("RUN_ENVIRONMENT");

            var configuration =  new ConfigurationBuilder()                
                .SetBasePath(projectDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            builder.RegisterInstance<IConfiguration>(configuration);
        }
    }
}
