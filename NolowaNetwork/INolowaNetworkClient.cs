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
        bool Connect(NetworkConfigurationModel configuration);
        Task ReceiveAsync(NetMessageBase message);
    }

    public interface INolowaNetworkSendable : INolowaNetworkClient
    {
        void Send<T>(T message) where T : NetMessageBase;
    }
}
