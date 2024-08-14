using NolowaNetwork.Models.Configuration;
using NolowaNetwork.Models.Message;

namespace NolowaNetwork
{
    public interface INolowaNetworkClient
    {
        bool Connect(NetworkConfigurationModel configuration);
        void Send<T>(T message) where T : NetMessageBase;
        Task ReceiveAsync(NetMessageBase message);
    }
}
