using Autofac;
using Microsoft.Extensions.Configuration;
using NolowaNetwork.RabbitMQNetwork;
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

            builder.RegisterType<RabbitNetworkClient>().As<INolowaNetworkClient>().SingleInstance();
            builder.RegisterType<MessageMaker>().As<IMessageMaker>();
            builder.RegisterType<MessageCodec>().As<IMessageCodec>();
            builder.RegisterType<MessageTypeResolver>().As<IMessageTypeResolver>();
            builder.RegisterType<MessageBroker>().As<IMessageBroker>().InstancePerLifetimeScope();

            SetConfiguration(builder);
        }

        public void SetConfiguration(ContainerBuilder builder)
        {
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
