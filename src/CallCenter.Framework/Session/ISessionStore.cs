namespace CallCenter.Framework.Session;

/// <summary>
/// 会话存储接口。
/// 主要作用：抽象会话存储的实现细节，支持内存/Redis 等多种后端。
/// 使用两层命名空间：scope（如 sessionId）→ key → value。
/// </summary>
public interface ISessionStore
{
    /// <summary>从会话存储中获取指定键的值。</summary>
    Task<T?> GetAsync<T>(string key, string? scope = null, CancellationToken ct = default);

    /// <summary>将值存入会话存储。</summary>
    /// <param name="ttl">可选的过期时间（仅 Redis 等支持 TTL 的存储后端生效）</param>
    Task SetAsync<T>(string key, T value, string? scope = null, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>尝试获取值，区分「key 不存在」和「反序列化失败」。</summary>
    /// <returns>成功获取到值时返回 (true, value)；key 不存在或反序列化失败时返回 (false, default)</returns>
    Task<(bool Success, T? Value)> TryGetAsync<T>(string key, string? scope = null, CancellationToken ct = default);

    /// <summary>从会话存储中移除指定键。</summary>
    Task RemoveAsync(string key, string? scope = null, CancellationToken ct = default);

    /// <summary>返回指定作用域中的所有键。</summary>
    Task<HashSet<string>> GetKeysAsync(string? scope = null, CancellationToken ct = default);

    /// <summary>清除指定作用域中的所有数据。</summary>
    Task ClearScopeAsync(string? scope = null, CancellationToken ct = default);
}
