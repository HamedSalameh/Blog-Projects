using StackExchange.Redis;

namespace RateLimitingWithRedis
{
    public class RedisRateLimiter
    {
        private readonly IDatabase _redis;
        private readonly int _limit;
        private readonly TimeSpan _window;

        public RedisRateLimiter(IConnectionMultiplexer connectionMultiplexer, int limit, TimeSpan window)
        {
            _redis = connectionMultiplexer.GetDatabase();
            _limit = limit;
            _window = window;
        }

        public async Task<bool> IsAllowedAsync(string key)
        {
            var redisKey = $"rate_limit:{key}";
            var count = await _redis.StringIncrementAsync(redisKey);

            if (count == 1)
            {
                // First request, set expiration for the window
                await _redis.KeyExpireAsync(redisKey, _window);
            }

            return count <= _limit;
        }
    }
}
