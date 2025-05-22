using StackExchange.Redis;

namespace RateLimitingWithRedis
{
    public class SlidingWindowLuaRateLimiter
    {
        private readonly IDatabase _redis;
        private readonly ILogger<SlidingWindowLuaRateLimiter> _logger;
        private readonly LuaScript _script;
        private readonly int _limit;
        private readonly TimeSpan _window;

        public SlidingWindowLuaRateLimiter(
            IConnectionMultiplexer connectionMultiplexer, ILogger<SlidingWindowLuaRateLimiter> logger, int limit, TimeSpan window)
        {
            _redis = connectionMultiplexer.GetDatabase();
            _logger = logger;
            _limit = limit;
            _window = window;

            // Atomic Lua script: removes old requests, counts existing ones,
            // adds new one if limit not exceeded, and sets expiry
            _script = LuaScript.Prepare(@"
            local key = KEYS[1]
            local now = tonumber(ARGV[1])
            local window = tonumber(ARGV[2])
            local limit = tonumber(ARGV[3])
            local expire = tonumber(ARGV[4])

            redis.call('ZREMRANGEBYSCORE', key, 0, now - window)
            local count = redis.call('ZCARD', key)
            if count >= limit then
                return 0
            else
                redis.call('ZADD', key, now, now .. '-' .. math.random())
                redis.call('EXPIRE', key, expire)
                return 1
            end
        ");
        }

        /// <summary>
        /// Atomically determines whether a request is allowed.
        /// </summary>
        public async Task<bool> IsAllowedAsync(string key)
        {
            var redisKey = new RedisKey[] { $"rate_limit:{key}" };
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var args = new RedisValue[]
            {
            now,                                // ARGV[1] = current timestamp
            (long)_window.TotalMilliseconds,    // ARGV[2] = window size
            _limit,                             // ARGV[3] = request limit
            (int)_window.TotalSeconds           // ARGV[4] = TTL
            };

            try
            {
                var result = (int)await _redis.ScriptEvaluateAsync(_script.OriginalScript, redisKey, args);
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SlidingWindowLuaRateLimiter failed for key {Key}", key);
                return true; // Optionally fail open
            }
        }
    }
}
