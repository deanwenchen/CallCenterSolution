// TODO: Production implementation using Redis for session persistence
// PRD Section 7.5.2.1: RedisSessionStore three-in-one (chat history + session serialization + checkpoint)
namespace CallCenter.Framework.Session;

public class RedisSessionStore
{
    // TODO: Implement Redis connection
    // TODO: Implement GetAsync/SetAsync/RemoveAsync with Redis
    // TODO: Implement TTL auto-expiration (30/90 days)
    // TODO: Implement chat history storage
    // TODO: Implement AgentSession serialization
    // TODO: Implement Workflow Checkpoint storage
    public Task<T?> GetAsync<T>(string key, string? scope = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("RedisSessionStore not implemented for Demo. Use InMemorySessionStore.");
    }

    public Task SetAsync<T>(string key, T value, string? scope = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("RedisSessionStore not implemented for Demo. Use InMemorySessionStore.");
    }

    public Task RemoveAsync(string key, string? scope = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("RedisSessionStore not implemented for Demo. Use InMemorySessionStore.");
    }
}
