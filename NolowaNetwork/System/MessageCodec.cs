using NolowaNetwork.Models.Message;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NolowaNetwork.System
{
    public interface IMessageCodec
    {
        string EncodeAsJson<T>(T message) where T : NetMessageBase;
        byte[] EncodeAsByte<T>(T message) where T : NetMessageBase;
        T? Decode<T>(ReadOnlyMemory<byte> input) where T : class;
    }

    public class MessageCodec : IMessageCodec
    {
        public string EncodeAsJson<T>(T message) where T : NetMessageBase
        {
            return JsonSerializer.Serialize(message);
        }

        public byte[] EncodeAsByte<T>(T message) where T : NetMessageBase
        {
            return JsonSerializer.SerializeToUtf8Bytes(message);
        }
        public T? Decode<T>(ReadOnlyMemory<byte> input) where T : class
        {
            return JsonSerializer.Deserialize<T>(input.Span);
        }

    }
}
