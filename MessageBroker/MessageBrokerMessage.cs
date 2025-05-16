using org.wakanda.framework.Utilities;

namespace org.wakanda.framework.MessageBroker
{
    public class MessageBrokerMessage<T> where T : class
    {
        public required string MessageType { get; set; }
        public required T Payload { get; set; }
        public long Timestamp { get; set; } = UnixTimeHelper.CurrentUnixTimestamp();
        public string? CorrelationId { get; set; }
    }
}