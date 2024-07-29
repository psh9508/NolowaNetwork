using Microsoft.VisualBasic;
using NolowaNetwork.Models.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.System
{
    public interface IMessageMaker
    {
        T MakeStartMessage<T>(string target) where T : NetMessageBase, new();
        T MakeTakeMessage<T>(string target) where T : NetMessageBase, new();
        T MakeResponseMessage<T>(string target);
    }

    public class MessageMaker : IMessageMaker
    {
        public T MakeStartMessage<T>(string target) where T : NetMessageBase, new()
        {
            var message = new T();
            message.MessageType = typeof(T).Name;
            message.Destination = target;

            return message;
        }

        public T MakeTakeMessage<T>(string target) where T : NetMessageBase, new()
        {
            var message = new T();
            message.MessageType = typeof(T).Name;
            message.TakeId = Guid.NewGuid().ToString();
            message.Destination = target;

            return message;
        }

        public T MakeResponseMessage<T>(string target)
        {
            throw new NotImplementedException();
        }
    }
}
