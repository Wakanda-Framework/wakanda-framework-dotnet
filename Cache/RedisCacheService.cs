using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;
using org.wakanda.framework.Cache.Interface;

namespace org.wakanda.framework.Cache
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IConnectionMultiplexer redisConnection)
        {
            _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var db = _redisConnection.GetDatabase();
            string json = JsonSerializer.Serialize(value, _jsonOptions);
            await db.StringSetAsync(key, json, expiry);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var db = _redisConnection.GetDatabase();
            var json = await db.StringGetAsync(key);
            if (json.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task RemoveAsync(string key)
        {
            var db = _redisConnection.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var db = _redisConnection.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
    }
}