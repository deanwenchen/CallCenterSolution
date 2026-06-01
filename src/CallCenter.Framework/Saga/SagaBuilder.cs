// TODO: PRD Section 5.4 场景2 — Saga Compensation (failure compensation + retry strategy 1min/5min/30min)
namespace CallCenter.Framework.Saga;

public class SagaBuilder
{
    // TODO: Implement "if A fails, execute B compensation" pattern
    // TODO: Implement retry strategy: 1min, 5min, 30min
    public SagaBuilder OnFailure(string step, Func<Task> compensation) => this;
    public SagaBuilder WithRetry(int maxRetries, params TimeSpan[] delays) => this;
    public Task ExecuteAsync() => Task.CompletedTask;
}
