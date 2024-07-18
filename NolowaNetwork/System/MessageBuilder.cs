using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.System
{
    public interface IMessageBuilder
    {
        T StartStream<T>(string target);
    }

    public class MessageBuilder : IMessageBuilder
    {
        public T StartStream<T>(string target)
        {
            throw new NotImplementedException();
        }
    }
}
