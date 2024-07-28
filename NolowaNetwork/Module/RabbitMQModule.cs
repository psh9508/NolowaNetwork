using Autofac;
using NolowaNetwork.RabbitMQNetwork;
using NolowaNetwork.System;
using NolowaNetwork.System.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NolowaNetwork.System.Worker.RabbitWorker;

namespace NolowaNetwork.Module
{
    public class RabbitMQModule
    {
        public void RegisterModule(ContainerBuilder builder)
        {
            builder.RegisterType<RabbitWorker>().As<IWorker>()
                .OnActivated(async x => await x.Instance.StartAsync(CancellationToken.None))
                .SingleInstance();
            
            builder.RegisterType<RabbitNetworkClient>()
                .UsingConstructor(typeof(IMessageCodec), typeof(IMessageTypeResolver))
                .As<INolowaNetworkSendable>()
                .SingleInstance();

            //builder.RegisterType<RabbitNetworkClient>()
            //    .UsingConstructor(typeof(IMessageCodec), typeof(IMessageTypeResolver), typeof(IWorker))
            //    .As<INolowaNetworkReceivable>()
            //    .SingleInstance();

            builder.RegisterType<RabbitNetworkClient>().As<INolowaNetworkClient>().SingleInstance();
            builder.RegisterType<MessageCodec>().As<IMessageCodec>();
            builder.RegisterType<MessageTypeResolver>().As<IMessageTypeResolver>();
            builder.RegisterType<MessageBroker>().As<IMessageBroker>().InstancePerLifetimeScope();
        }
    }
}
