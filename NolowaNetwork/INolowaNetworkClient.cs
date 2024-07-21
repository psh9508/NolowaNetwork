using NolowaNetwork.Models.Configuration;
using NolowaNetwork.Models.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork
{
    public interface INolowaNetworkClient
    {
        bool Init(NetworkConfigurationModel configuration);
    }

    public interface INolowaNetworkSendable : INolowaNetworkClient
    {
        void Send<T>(T message) where T : NetMessageBase;
    }

    public interface INolowaNetworkReceivable : INolowaNetworkClient
    {
        Task ReceiveAsync(NetMessageBase message);
    }
}
