// TODO: Production implementation — see PRD Section 7.5.2.1
// RedisSessionStore three-in-one: chat history + session serialization + checkpoint
namespace CallCenter.Framework.Session;

/// <summary>
/// Redis 会话存储（占位符）。
/// 主要作用：在生产环境中把会话状态、聊天历史和工作流断点持久化到 Redis，支持恢复和过期管理。
/// 演示阶段未实现，当前使用 InMemorySessionStore 替代。
/// 生产环境需存储三项内容：
///   - 聊天历史（用于上下文压缩）
///   - Agent 会话序列化（用于恢复对话）
///   - 工作流断点（用于长运行工作流恢复）
///   - 支持自动过期（30/90 天可配置）
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
