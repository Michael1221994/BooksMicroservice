using StackExchange.Redis;
using System.Threading.Tasks;

namespace ReviewService.Services
{
    public class RedisCacheService
    {
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer connection)
        {
            _db = connection.GetDatabase();
        }

        public async Task<string?> GetCachedValue(string key)
        {
            return await _db.StringGetAsync(key);
        }

        public async Task SetCachedValue(string key, string value, TimeSpan expiration)
        {
            await _db.StringSetAsync(key, value, expiration);
        }
    }
}
