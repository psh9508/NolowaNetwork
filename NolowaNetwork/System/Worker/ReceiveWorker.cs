using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.System.Worker
{
    internal class ReceiveWorker : WorkerBase
    {
        protected override Task ReceiveAsync(dynamic message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
