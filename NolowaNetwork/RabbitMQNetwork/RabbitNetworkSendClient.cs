using NolowaNetwork.Models.Message;
using NolowaNetwork.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolowaNetwork.RabbitMQNetwork
{
    /// <summary>
    /// RabbitNetworkSendClient
    /// RabbitNetworkClient의 partial 클래스
    /// </summary>
    public partial class RabbitNetworkClient
    {
        // sender용 worker가 필요 없음
        public RabbitNetworkClient(IMessageCodec messageCodec, IMessageTypeResolver messageTypeResolver)
        {
            _messageCodec = messageCodec;
            _messageTypeResolver = messageTypeResolver;
        }

        public void Send<T>(T message) where T : NetMessageBase
        {
            try
            {
                var channel = _connection.CreateModel();

                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent;

                //var sendMessage = new NetSendMessage()
                //{
                //    MessageType = message.MessageType,
                //    JsonPayload = message.JsonPayload,
                //    Destination = message.Destination,
                //};

                var messagePayload = _messageCodec.EncodeAsByte(message);

                var routingKey = $"{_serverName}.{message.Destination}.{nameof(NetSendMessage)}";

                channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: messagePayload
                );
            }
            catch (Exception ex)
            {
            }
        }
    }
}
