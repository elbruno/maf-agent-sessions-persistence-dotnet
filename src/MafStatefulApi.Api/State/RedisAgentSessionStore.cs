using Microsoft.Extensions.Caching.Distributed;

namespace MafStatefulApi.Api.State;

/// <summary>
/// Redis-based implementation of agent session store using IDistributedCache.
/// Suitable for production and multi-instance deployments.
/// </summary>
public class RedisAgentSessionStore : IAgentSessionStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisAgentSessionStore> _logger;
    private readonly TimeSpan _sessionTtl;

    public RedisAgentSessionStore(
        IDistributedCache cache,
        ILogger<RedisAgentSessionStore> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        var ttlMinutes = configuration.GetValue("SessionTtlMinutes", 30);
        _sessionTtl = TimeSpan.FromMinutes(ttlMinutes);
    }

    public async Task<string?> GetAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId);
        var value = await _cache.GetStringAsync(key, cancellationToken);
        
        _logger.LogInformation(
            "Redis cache {CacheHitOrMiss} for conversation {ConversationId}",
            value != null ? "hit" : "miss",
            conversationId);
        
        return value;
    }

    public async Task SetAsync(string conversationId, string serializedThread, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId);
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = _sessionTtl
        };
        
        await _cache.SetStringAsync(key, serializedThread, options, cancellationToken);
        
        _logger.LogInformation(
            "Redis cache stored session for conversation {ConversationId}, size: {SizeBytes} bytes",
            conversationId,
            serializedThread.Length);
    }

    public async Task DeleteAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId);
        await _cache.RemoveAsync(key, cancellationToken);
        
        _logger.LogInformation(
            "Redis cache deleted session for conversation {ConversationId}",
            conversationId);
    }

    private static string GetKey(string conversationId) => $"maf:sessions:{conversationId}";
}
