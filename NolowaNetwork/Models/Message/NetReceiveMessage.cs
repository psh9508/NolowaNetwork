namespace NolowaNetwork.Models.Message
{
    public class NetReceiveMessage : NetMessageBase
    {
        public NetReceiveMessage(NetMessageBase message)
        {
            this.TakeId = message.TakeId;
            this.IsResponsMessage = message.IsResponsMessage;
            this.MessageType = message.MessageType;
            this.JsonPayload = message.JsonPayload;
            this.Destination = message.Destination;
        }
    }
}
