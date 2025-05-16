using System;

namespace org.wakanda.framework.MessageBroker
{
    public class MessageBrokerPublishException : Exception
    {
        public MessageBrokerPublishException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}

