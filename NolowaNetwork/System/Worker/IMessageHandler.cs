using NolowaNetwork.Models.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.System.Worker
{
    public interface IMessageHandler
    {
        Task HandleAsync(NetMessageBase message, CancellationToken cancellationToken);
    }
}
