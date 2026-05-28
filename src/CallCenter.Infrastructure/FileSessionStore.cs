using System.Text.Json;
using System.Text.Json.Serialization;
using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

public sealed class FileSessionStore : ISessionStore
{
    private readonly DirectoryInfo _directory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FileSessionStore()
    {
        // 会话状态独立于 MAF checkpoint，保存“哪个会话正在哪个 Workflow/Step”。
        _directory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "session-state"));
        _directory.Create();
    }

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
