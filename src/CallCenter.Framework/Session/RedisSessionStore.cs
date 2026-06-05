using Leo.Data.Redis;

namespace CallCenter.Framework.Session;

/// <summary>
/// Redis 会话存储。
/// 主要作用：在生产环境中把会话状态持久化到 Redis，支持恢复和过期管理。
/// 复用 Leo.Data.Redis 的 RedisHelper 静态 API，序列化由 ServiceStack.Text 内部处理。
/// 存储三项内容：
///   - 聊天历史（用于上下文压缩）
///   - Agent 会话序列化（用于恢复对话）
///   - 工作流断点（用于长运行工作流恢复）
///   - 支持自动过期（可配置 TTL）
/// </summary>
public class RedisSessionStore : ISessionStore
{
    private readonly string _providerName;
    private readonly int _dbIndex;
    private readonly TimeSpan? _defaultTtl;

    /// <summary>
    /// 创建 Redis 会话存储实例。
    /// </summary>
    /// <param name="providerName">Leo.Data.Redis 的 provider 名称（对应 redisconfig.json 中的配置）</param>
    /// <param name="dbIndex">Redis 数据库索引</param>
    /// <param name="defaultTtl">默认 TTL（SetAsync 未指定 ttl 时使用）</param>
    public RedisSessionStore(string providerName = "default", int dbIndex = 0, TimeSpan? defaultTtl = null)
    {
        _providerName = providerName;
        _dbIndex = dbIndex;
        _defaultTtl = defaultTtl;
    }

    /// <summary>拼接 scope:key 格式的 Redis 键名。</summary>
    private static string MakeKey(string key, string? scope)
    {
        var scopeKey = scope ?? "default";
        return $"{scopeKey}:{key}";
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, string? scope = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var redisKey = MakeKey(key, scope);
        return await RedisHelper.GetAsync<T>(redisKey, _providerName, _dbIndex).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(bool Success, T? Value)> TryGetAsync<T>(string key, string? scope = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var redisKey = MakeKey(key, scope);

        var rawValue = await RedisHelper.GetValueAsync(redisKey, _providerName, _dbIndex).ConfigureAwait(false);
        if (rawValue == null)
        {
            return (false, default);
        }

        try
        {
            var value = await RedisHelper.GetAsync<T>(redisKey, _providerName, _dbIndex).ConfigureAwait(false);
            return value != null ? (true, value) : (false, default);
        }
        catch
        {
            return (false, default);
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, string? scope = null, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var redisKey = MakeKey(key, scope);
        var effectiveTtl = ttl ?? _defaultTtl;

        if (effectiveTtl.HasValue)
        {
            await RedisHelper.SetAsync(redisKey, value, effectiveTtl.Value, _providerName, _dbIndex).ConfigureAwait(false);
        }
        else
        {
            await RedisHelper.SetAsync(redisKey, value, _providerName, _dbIndex).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, string? scope = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var redisKey = MakeKey(key, scope);
        await redisKey.RemoveAsync(_providerName, _dbIndex).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> GetKeysAsync(string? scope = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var pattern = $"{scope ?? "default"}:*";
        var keys = new HashSet<string>();

        await using (var client = await _providerName.GetClientAsync(_dbIndex).ConfigureAwait(false))
        {
            var allKeys = await client.SearchKeysAsync(pattern).ConfigureAwait(false);
            var prefix = $"{scope ?? "default"}:";
            foreach (var fullKey in allKeys)
            {
                if (fullKey.StartsWith(prefix))
                {
                    keys.Add(fullKey.Substring(prefix.Length));
                }
            }
        }

        return keys;
    }

    /// <inheritdoc />
    public async Task ClearScopeAsync(string? scope = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var prefix = $"{scope ?? "default"}:";

        await using (var client = await _providerName.GetClientAsync(_dbIndex).ConfigureAwait(false))
        {
            var allKeys = await client.SearchKeysAsync($"{prefix}*").ConfigureAwait(false);
            if (allKeys.Count > 0)
            {
                await client.RemoveAllAsync(allKeys).ConfigureAwait(false);
            }
        }
    }
}
