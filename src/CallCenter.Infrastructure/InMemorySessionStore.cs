using System.Collections.Concurrent;
using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, WorkflowState> _states = new(StringComparer.OrdinalIgnoreCase);

    public Task<WorkflowState?> GetActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _states.TryGetValue(sessionId, out WorkflowState? state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(WorkflowState state, CancellationToken cancellationToken = default)
    {
        _states[state.SessionId] = state;
        return Task.CompletedTask;
    }

    public Task ClearActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _states.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
