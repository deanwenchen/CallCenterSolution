// TODO: Production implementation — see PRD Section 7.5.2.1
// RedisSessionStore three-in-one: chat history + session serialization + checkpoint
namespace CallCenter.Framework.Session;

/// <summary>
/// Placeholder for Redis-backed session persistence.
/// In production, this will store:
///   - Chat history for context window compaction
///   - Serialized agent sessions for resume
///   - Workflow checkpoints for long-running workflows
///   - TTL auto-expiration (30/90 days configurable)
/// Currently throws NotImplementedException — use InMemorySessionStore for demo.
/// </summary>
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
