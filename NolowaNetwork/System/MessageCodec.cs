using NolowaNetwork.Models.Message;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NolowaNetwork.System
{
    public interface IMessageCodec
    {
        string EncodeAsJson<T>(T message) where T : NetMessageBase;
        string EncodeAsJson(object message, Type type);
        byte[] EncodeAsByte<T>(T message) where T : NetMessageBase;
        byte[] EncodeAsByte(object message, Type type);
        T? Decode<T>(ReadOnlyMemory<byte> input) where T : class;
        T? DecodeJson<T>(NetMessageBase message) where T : class;
    }

    public class MessageCodec : IMessageCodec
    {
        public string EncodeAsJson<T>(T message) where T : NetMessageBase
        {
            return JsonSerializer.Serialize(message);
        }

        public string EncodeAsJson(object message, Type type)
        {
            return JsonSerializer.Serialize(message, type);
        }

        public byte[] EncodeAsByte<T>(T message) where T : NetMessageBase
        {
            return JsonSerializer.SerializeToUtf8Bytes(message);
        }

        public byte[] EncodeAsByte(object message, Type type)
        {
            return JsonSerializer.SerializeToUtf8Bytes(message, type);
        }

        public T? Decode<T>(ReadOnlyMemory<byte> input) where T : class
        {
            return JsonSerializer.Deserialize<T>(input.Span);
        }

        public T? DecodeJson<T>(NetMessageBase message) where T : class
        {
            var jsonPayloadBytes = Encoding.UTF8.GetBytes(message.JsonPayload);
            var readOnlyMemory = new ReadOnlyMemory<byte>(jsonPayloadBytes);
            
            return JsonSerializer.Deserialize<T>(readOnlyMemory.Span);
        }
    }
}
