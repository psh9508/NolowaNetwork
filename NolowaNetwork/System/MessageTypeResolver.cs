using NolowaNetwork.Models.Message;

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
            //if (typeName is "TestMessage")
            //    return typeof(TestMessage);
            //else if(typeName is "NetSendMessage")
            //    return typeof(NetSendMessage);

            if (typeName is "NetSendMessage")
                return typeof(NetSendMessage);

            return null;
        }

        public dynamic GetTypeByDynamic(string typeName)
        {
            throw new NotImplementedException();
        }
    }
}
