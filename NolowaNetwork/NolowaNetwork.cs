using NolowaNetwork.Models.Configuration;
using NolowaNetwork.Models.Message;
using NolowaNetwork.System.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NolowaNetwork.System.Worker.RabbitWorker;

namespace NolowaNetwork
{
    public class NolowaNetwork : INolowaNetwork
    {
        private readonly IWorker _worker;
        private readonly INolowaNetworkClient _netClient;

        public NolowaNetwork(IWorker worker, INolowaNetworkClient netClient)
        {
            _worker = worker;
            _netClient = netClient;
        }

        public bool Init(NetworkConfigurationModel configuration)
        {
            return _netClient.Init(configuration);
        }

        public async Task SendAsync<T>(T message) where T : NetMessageBase
        {
            await _worker.QueueMessageAsync(ERabbitWorkerType.SENDER.ToString(), message);
        }
    }
}
