using System.Text.Json;
using System.Text.Json.Serialization;
using CallCenter.Core;

namespace CallCenter.Infrastructure;

/// <summary>
/// 基于本地 JSON 文件的会话状态存储。
/// </summary>
public sealed class FileSessionStore : ISessionStore
{
    private readonly DirectoryInfo _directory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// 初始化本地会话状态目录。
    /// </summary>
    public FileSessionStore()
    {
        // 会话状态独立于 MAF checkpoint，保存“哪个会话正在哪个 Workflow/Step”。
        _directory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "session-state"));
        _directory.Create();
    }

    /// <summary>
    /// 从本地文件读取会话当前活动 Workflow 状态。
    /// </summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>活动 Workflow 状态；不存在时返回 null。</returns>
    public async Task<WorkflowState?> GetActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        string path = GetPath(sessionId);
        if (!File.Exists(path))
        {
            return null;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<WorkflowState>(stream, _jsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 将会话当前活动 Workflow 状态写入本地文件。
    /// </summary>
    /// <param name="state">Workflow 状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task SaveAsync(WorkflowState state, CancellationToken cancellationToken = default)
    {
        string path = GetPath(state.SessionId);
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using FileStream stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, state, _jsonOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 删除会话当前活动 Workflow 状态文件。
    /// </summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task ClearActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        string path = GetPath(sessionId);
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetPath(string sessionId)
    {
        string safeSessionId = Uri.EscapeDataString(sessionId).Replace(".", "%2E", StringComparison.Ordinal);
        return Path.Combine(_directory.FullName, $"{safeSessionId}.json");
    }
}
