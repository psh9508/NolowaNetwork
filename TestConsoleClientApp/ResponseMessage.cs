using NolowaNetwork.Models.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleClientApp
{
    internal class ResponseMessage : NetMessageBase
    {
        public string Message { get; set; } = string.Empty;
    }
}
