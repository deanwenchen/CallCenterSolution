using System.Collections.Concurrent;
using System.Text.Json;

namespace CallCenter.Framework.Session;

/// <summary>
/// 内存会话存储。使用两层字典：scope（如 sessionId）→ key → value。
/// 线程安全（ConcurrentDictionary）。适用于演示/开发环境。
/// 生产环境应使用 RedisSessionStore 实现持久化存储。
/// 用于存储：activeWorkflow（当前工作流名）、lastCheckpoint（断点信息）、
/// lastActivity（最后活动时间）、pendingOrderId（动态追问的订单号）等。
/// </summary>
public class InMemorySessionStore
{
    // Outer dictionary: scope (e.g., sessionId) → inner dictionary of key-value pairs
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _store = new();

    /// <summary>Retrieves a typed value from the session store.</summary>
    public Task<T?> GetAsync<T>(string key, string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        if (_store.TryGetValue(scopeKey, out var dict) && dict.TryGetValue(key, out var value))
        {
            if (value is T typed) return Task.FromResult<T?>(typed);
        }
        return Task.FromResult<T?>(default);
    }

    /// <summary>Stores a typed value in the session store.</summary>
    public Task SetAsync<T>(string key, T value, string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        var dict = _store.GetOrAdd(scopeKey, _ => new ConcurrentDictionary<string, object>());
        dict[key] = value!;
        return Task.CompletedTask;
    }

    /// <summary>Removes a key from the session store.</summary>
    public Task RemoveAsync(string key, string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        if (_store.TryGetValue(scopeKey, out var dict))
        {
            dict.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    /// <summary>Returns all keys in the given scope.</summary>
    public Task<HashSet<string>> GetKeysAsync(string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        if (_store.TryGetValue(scopeKey, out var dict))
        {
            return Task.FromResult(dict.Keys.ToHashSet());
        }
        return Task.FromResult(new HashSet<string>());
    }

    /// <summary>Removes all data in the given scope.</summary>
    public Task ClearScopeAsync(string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        _store.TryRemove(scopeKey, out _);
        return Task.CompletedTask;
    }
}
