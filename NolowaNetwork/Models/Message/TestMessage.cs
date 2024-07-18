using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.Models.Message
{
    public class TestMessage : NetMessageBase
    {
        public string Message { get; set; } = string.Empty;
    }
}
