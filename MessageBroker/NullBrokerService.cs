using System.Threading.Tasks;
using org.wakanda.framework.MessageBroker.Interface;

namespace org.wakanda.framework.MessageBroker
{
    public class NullBrokerService : IBrokerService
    {
        public Task PublishAsync<T>(string topic, MessageBrokerMessage<T> message) where T : class
            => Task.CompletedTask;
    }
}