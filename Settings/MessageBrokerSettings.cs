namespace org.wakanda.framework.Settings
{
    public class MessageBrokerSettings
    {
        //Master toggle for message broker.
        public bool Enabled { get; set; } = false;
        //Provider key: "Kafka", "MQ", "Mule", etc
        public string Provider { get; set; } = "Null";
        //Connection string or bootstrap servers
        public string ConnectionString { get; set; } = string.Empty;
    }
}