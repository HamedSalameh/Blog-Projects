using StackExchange.Redis;

namespace RateLimitingWithRedis
{
    public class SlidingWindowRateLimiter
    {
        private readonly IDatabase _redis;
        private readonly ILogger<SlidingWindowRateLimiter> _logger;
        private readonly int _limit;
        private readonly TimeSpan _window;

        public SlidingWindowRateLimiter(IConnectionMultiplexer connectionMultiplexer, ILogger<SlidingWindowRateLimiter> logger, int limit, TimeSpan window)
        {
            _redis = connectionMultiplexer.GetDatabase() ?? throw new InvalidOperationException("Unable to get Redis database.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _limit = limit;
            _window = window;
        }

        public async Task<bool> IsAllowedAsync(string key)
        {
            var redisKey = $"rate_limit:{key}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (long)_window.TotalMilliseconds;

            // Remove expired timestamps
            try
            {
                // step 1 : Remove expired timestamps that are outside the sliding window
                await _redis.SortedSetRemoveRangeByScoreAsync(redisKey, 0, windowStart);

                // Step 2 : Count the current entries (requests) in the window
                var currentCount = await _redis.SortedSetLengthAsync(redisKey);
                if ( currentCount >= _limit )
                {
                    _logger.LogWarning("Rate limit exceeded for key: {Key}. Current count: {CurrentCount}, Limit: {Limit}", key, currentCount, _limit);
                    return false;
                }

                // Step 3 : Add the new request with timestamp as score
                var requestId = $"{now}-{Guid.NewGuid()}";  // prevent duplicate entries
                await _redis.SortedSetAddAsync(redisKey, requestId, now);

                // Step 4 : Set expiration for the sliding window
                await _redis.KeyExpireAsync(redisKey, _window);

                _logger.LogInformation("Request allowed for key: {Key}. Current count: {CurrentCount}, Limit: {Limit}", key, currentCount + 1, _limit);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while removing expired timestamps from Redis.");
                return false;
            }

        }
    }
}
