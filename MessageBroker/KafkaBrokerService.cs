using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using org.wakanda.framework.MessageBroker.Interface;
using org.wakanda.framework.Settings;
using Microsoft.Extensions.Options;

namespace org.wakanda.framework.MessageBroker
{
    public class KafkaBrokerService : IBrokerService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        public KafkaBrokerService(IOptions<MessageBrokerSettings> opts)
        {
            var cfg = new ProducerConfig
            {
                BootstrapServers = opts.Value.ConnectionString
            };
            _producer = new ProducerBuilder<string, string>(cfg).Build();
        }

        public async Task PublishAsync<T>(string topic, MessageBrokerMessage<T> message) where T : class
        {
            var payload = JsonSerializer.Serialize(message);
            try
            {
                await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key   = message.CorrelationId,
                    Value = payload
                });
            }
            catch (Exception ex)
            {
                throw new MessageBrokerPublishException(
                    $"Failed to publish to topic '{topic}'", ex);
            }
        }

        public void Dispose() => _producer?.Dispose();
    }
}