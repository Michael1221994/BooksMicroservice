using StackExchange.Redis;

namespace BookService.Services
{
    public class RedisCacheService
    {
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<string?> GetCachedValue(string key)
        {
            return await _db.StringGetAsync(key);
        }

        public async Task SetCachedValue(string key, string value, TimeSpan? expiry = null)
        {
            await _db.StringSetAsync(key, value, expiry);
        }

        public async Task RemoveCachedValue(string key)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}