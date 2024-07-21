using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.Models.Message
{
    public class NetMessageBase
    {
        public string MessageType { get; set; } = string.Empty;
        public string JsonPayload { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
    }
}
