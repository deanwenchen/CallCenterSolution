using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using CallCenter.Core;

namespace CallCenter.Infrastructure;

/// <summary>
/// 基于请求元数据的本地认证实现。
/// </summary>
public sealed class MetadataAuthenticationService : IAuthenticationService
{
    /// <summary>
    /// 检查请求认证令牌，当前仅拒绝显式传入的 invalid token。
    /// </summary>
    /// <param name="request">会话消息请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>认证结果。</returns>
    public Task<AccessDecision> AuthenticateAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.AuthToken, "invalid", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new AccessDecision(AccessDecisionStatus.Denied, "Authentication token is invalid."));
        }

        Dictionary<string, string> attributes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["authMode"] = string.IsNullOrWhiteSpace(request.AuthToken) ? "anonymous" : "token"
        };

        return Task.FromResult(new AccessDecision(AccessDecisionStatus.Allowed, "Authenticated.", attributes));
    }
}

/// <summary>
/// 基于请求元数据的本地鉴权实现。
/// </summary>
public sealed class MetadataAuthorizationService : IAuthorizationService
{
    /// <summary>
    /// 检查请求是否被元数据标记为 forbidden。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="request">会话消息请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>鉴权结果。</returns>
    public Task<AccessDecision> AuthorizeAsync(
        SessionContext session,
        ConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Metadata?.TryGetValue("forbidden", out string? forbidden) == true &&
            bool.TryParse(forbidden, out bool isForbidden) &&
            isForbidden)
        {
            return Task.FromResult(new AccessDecision(AccessDecisionStatus.Denied, "Request is not authorized."));
        }

        return Task.FromResult(new AccessDecision(AccessDecisionStatus.Allowed, "Authorized."));
    }
}

/// <summary>
/// 基于会话属性的本地黑名单检查实现。
/// </summary>
public sealed class MetadataBlacklistService : IBlacklistService
{
    /// <summary>
    /// 检查 session attributes 中的 blacklisted 标记。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>黑名单检查结果。</returns>
    public Task<AccessDecision> CheckAsync(SessionContext session, CancellationToken cancellationToken = default)
    {
        if (session.Attributes?.TryGetValue("blacklisted", out string? value) == true &&
            bool.TryParse(value, out bool blacklisted) &&
            blacklisted)
        {
            return Task.FromResult(new AccessDecision(AccessDecisionStatus.Denied, "User or session is blacklisted."));
        }

        return Task.FromResult(new AccessDecision(AccessDecisionStatus.Allowed, "Not blacklisted."));
    }
}

/// <summary>
/// 固定窗口限流器，按租户、用户和渠道维度限流。
/// </summary>
public sealed class FixedWindowRateLimiter : IRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private const int Limit = 60;

    private readonly ConcurrentDictionary<string, Bucket> _buckets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 检查当前会话是否超过固定窗口请求限制。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>限流检查结果。</returns>
    public Task<RateLimitDecision> CheckAsync(SessionContext session, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string key = $"{session.TenantId}:{session.UserId}:{session.Channel}";
        Bucket bucket = _buckets.AddOrUpdate(
            key,
            _ => new Bucket(now, 1),
            (_, current) => now - current.WindowStarted >= Window
                ? new Bucket(now, 1)
                : current with { Count = current.Count + 1 });

        int remaining = Math.Max(0, Limit - bucket.Count);
        if (bucket.Count > Limit)
        {
            TimeSpan retryAfter = Window - (now - bucket.WindowStarted);
            return Task.FromResult(new RateLimitDecision(false, Limit, 0, retryAfter, "Rate limit exceeded."));
        }

        return Task.FromResult(new RateLimitDecision(true, Limit, remaining, TimeSpan.Zero, "Allowed."));
    }

    private sealed record Bucket(DateTimeOffset WindowStarted, int Count);
}

/// <summary>
/// 将审计事件写入本地 ndjson 文件的实现。
/// </summary>
public sealed class FileAuditSink : IAuditSink
{
    private readonly DirectoryInfo _directory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// 初始化审计日志目录。
    /// </summary>
    public FileAuditSink()
    {
        _directory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "audit-log"));
        _directory.Create();
    }

    /// <summary>
    /// 追加写入一条审计事件。
    /// </summary>
    /// <param name="auditEvent">审计事件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        string path = Path.Combine(_directory.FullName, $"{DateTimeOffset.UtcNow:yyyyMMdd}.ndjson");
        string line = JsonSerializer.Serialize(auditEvent, _jsonOptions);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}

/// <summary>
/// 将观测事件写入本地日志文件的实现。
/// </summary>
public sealed class FileObservabilitySink : IObservabilitySink
{
    private readonly DirectoryInfo _directory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// 初始化观测日志目录。
    /// </summary>
    public FileObservabilitySink()
    {
        _directory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "observability"));
        _directory.Create();
    }

    /// <summary>
    /// 追加写入一条观测事件。
    /// </summary>
    /// <param name="name">事件名称。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="properties">事件属性。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task TrackAsync(
        string name,
        SessionContext session,
        Dictionary<string, string> properties,
        CancellationToken cancellationToken = default)
    {
        string path = Path.Combine(_directory.FullName, $"{DateTimeOffset.UtcNow:yyyyMMdd}.log");
        string fields = string.Join(" ", properties.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
        string line = $"{DateTimeOffset.UtcNow:o} correlationId={session.CorrelationId} sessionId={session.SessionId} name={name} {fields}";

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}
