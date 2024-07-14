using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.System
{
    public interface IMessageTypeResolver
    {
        void AddType(Type type);
        Type? GetType(string typeName);
        dynamic GetTypeByDynamic(string typeName);
    }

    public class MessageTypeResolver : IMessageTypeResolver
    {
        public void AddType(Type type)
        {
            throw new NotImplementedException();
        }

        public Type? GetType(string typeName)
        {
            throw new NotImplementedException();
        }

        public dynamic GetTypeByDynamic(string typeName)
        {
            throw new NotImplementedException();
        }
    }
}
