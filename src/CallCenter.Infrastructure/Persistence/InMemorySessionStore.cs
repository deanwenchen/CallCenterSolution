using System.Collections.Concurrent;
using CallCenter.Core;

namespace CallCenter.Infrastructure;

/// <summary>
/// 基于内存字典的会话状态存储，适用于本地开发和单进程验证。
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, WorkflowState> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取会话当前活动 Workflow 状态。
    /// </summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>活动 Workflow 状态；不存在时返回 null。</returns>
    public Task<WorkflowState?> GetActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _states.TryGetValue(sessionId, out WorkflowState? state);
        return Task.FromResult(state);
    }

    /// <summary>
    /// 保存会话当前活动 Workflow 状态。
    /// </summary>
    /// <param name="state">Workflow 状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public Task SaveAsync(WorkflowState state, CancellationToken cancellationToken = default)
    {
        _states[state.SessionId] = state;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 清理会话当前活动 Workflow 状态。
    /// </summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public Task ClearActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _states.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
