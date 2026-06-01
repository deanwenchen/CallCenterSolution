using System.Collections.Concurrent;
using System.Text.Json;

namespace CallCenter.Framework.Session;

public class InMemorySessionStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _store = new();

    public Task<T?> GetAsync<T>(string key, string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        if (_store.TryGetValue(scopeKey, out var dict) && dict.TryGetValue(key, out var value))
        {
            if (value is T typed) return Task.FromResult<T?>(typed);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        var dict = _store.GetOrAdd(scopeKey, _ => new ConcurrentDictionary<string, object>());
        dict[key] = value!;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        if (_store.TryGetValue(scopeKey, out var dict))
        {
            dict.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public Task<HashSet<string>> GetKeysAsync(string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        if (_store.TryGetValue(scopeKey, out var dict))
        {
            return Task.FromResult(dict.Keys.ToHashSet());
        }
        return Task.FromResult(new HashSet<string>());
    }

    public Task ClearScopeAsync(string? scope = null, CancellationToken ct = default)
    {
        var scopeKey = scope ?? "default";
        _store.TryRemove(scopeKey, out _);
        return Task.CompletedTask;
    }
}
