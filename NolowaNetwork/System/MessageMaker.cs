using NolowaNetwork.Models.Message;

namespace NolowaNetwork.System
{
    public interface IMessageMaker
    {
        T MakeStartMessage<T>(string source, string target) where T : NetMessageBase, new();
        T MakeTakeMessage<T>(string source, string target) where T : NetMessageBase, new();
        T MakeResponseMessage<T>(string source, NetMessageBase originalMessage) where T : NetMessageBase, new();
    }

    public class MessageMaker : IMessageMaker
    {
        public T MakeStartMessage<T>(string source, string target) where T : NetMessageBase, new()
        {
            var message = new T();
            message.MessageType = typeof(T).Name;
            message.TakeId = string.Empty;
            message.Destination = target;
            message.Source = source;
            message.Origin = source;
            message.IsResponsMessage = false;

            return message;
        }

        public T MakeTakeMessage<T>(string source, string target) where T : NetMessageBase, new()
        {
            var message = new T();
            message.MessageType = typeof(T).Name;
            message.TakeId = Guid.NewGuid().ToString();
            message.Destination = target;
            message.Source = source;
            message.Origin = source;
            message.IsResponsMessage = false;

            return message;
        }

        public T MakeResponseMessage<T>(string source, NetMessageBase originalMessage) where T : NetMessageBase, new()
        {
            var message = new T();
            message.MessageType = originalMessage.GetType().Name;
            message.TakeId = originalMessage.TakeId;
            message.Origin = originalMessage.Origin;
            message.Source = source;
            message.Destination = originalMessage.Origin; // 받은 곳으로 돌려준다.
            message.IsResponsMessage = true;

            return message;
        }
    }
}
