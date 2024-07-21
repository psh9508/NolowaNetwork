using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.Models.Message
{
    public class NetReceiveMessage : NetMessageBase
    {
        public NetReceiveMessage(NetMessageBase message)
        {
            this.MessageType = message.MessageType;
            this.JsonPayload = message.JsonPayload;
            this.Destination = message.Destination;
        }
    }
}
