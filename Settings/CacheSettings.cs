namespace org.wakanda.framework.Settings
{
    public class CacheSettings
    {
        // Master toggle for caching.
        public bool Enabled { get; set; } = false;

        // Provider name: "Redis" or others.
        public string Provider { get; set; } = "Null";


        //Default TTL in minutes.
        public int DefaultExpiryMinutes { get; set; } = 30;
    }
}