using NolowaNetwork.Models.Message;
using System.Text.Json;

namespace NolowaNetwork.System
{
    public interface IMessageCodec
    {
        byte[] Encode<T>(INetMessage message) where T : class;
        T? Decode<T>(ReadOnlySpan<byte> input) where T : class;
    }

    public class MessageCodec : IMessageCodec
    {
        byte[] IMessageCodec.Encode<T>(INetMessage message) where T : class
        {
            return JsonSerializer.SerializeToUtf8Bytes(message);
        }

        T? IMessageCodec.Decode<T>(ReadOnlySpan<byte> input) where T : class
        {
            return JsonSerializer.Deserialize<T>(input);
        }
    }
}
