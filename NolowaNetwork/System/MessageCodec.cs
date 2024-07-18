using NolowaNetwork.Models.Message;
using System.Text.Json;

namespace NolowaNetwork.System
{
    public interface IMessageCodec
    {
        byte[] Encode<T>(T message) where T : class;
        T? Decode<T>(ReadOnlyMemory<byte> input) where T : class;
    }

    public class MessageCodec : IMessageCodec
    {
        public byte[] Encode<T>(T message) where T : class
        {
            return JsonSerializer.SerializeToUtf8Bytes(message);
        }

        public T? Decode<T>(ReadOnlyMemory<byte> input) where T : class
        {
            return JsonSerializer.Deserialize<T>(input.Span);
        }
    }
}
