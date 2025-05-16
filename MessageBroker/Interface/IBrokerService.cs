using System.Threading.Tasks;

namespace org.wakanda.framework.MessageBroker.Interface
{
    public interface IBrokerService
    {
        //Publish a JSON-serialized message to a topic
        Task PublishAsync<T>(string topic, MessageBrokerMessage<T> message) where T : class;
    }
}