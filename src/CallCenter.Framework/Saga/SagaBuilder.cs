using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CallCenter.Framework.Saga;

/// <summary>Saga 补偿异常。重试耗尽且补偿也失败时抛出。</summary>
public class SagaCompensationException : Exception
{
    public string FailedStep { get; }
    public string CompensationStep { get; }
    public Exception OriginalException { get; }

    public SagaCompensationException(string failedStep, string compensationStep, Exception original, string message)
        : base(message, original)
    {
        FailedStep = failedStep;
        CompensationStep = compensationStep;
        OriginalException = original;
    }
}

/// <summary>
/// Saga 构建器。
/// 主要作用：为有副作用的业务步骤提供“失败重试 + 最终补偿回滚”能力，避免流程半成功半失败。
/// 重试 + 补偿模式。失败时先重试，耗尽后执行补偿回滚。
/// </summary>
public class SagaBuilder
{
    private readonly List<(string Step, Func<CancellationToken, Task> Compensation)> _compensations = new();
    private int _maxRetries = 3;
    private TimeSpan[] _delays = { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30) };

    public SagaBuilder OnFailure(string step, Func<CancellationToken, Task> compensation)
    {
        _compensations.Add((step, compensation));
        return this;
    }

    public SagaBuilder WithRetry(int maxRetries, params TimeSpan[] delays)
    {
        _maxRetries = maxRetries;
        _delays = delays;
        return this;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        var lastException = (Exception?)null;

        // Try the action with retries
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await action(ct);
                return; // Success
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < _maxRetries && attempt < _delays.Length)
                {
                    await Task.Delay(_delays[attempt], ct);
                }
            }
        }

        // All retries exhausted — execute compensation
        foreach (var (step, compensation) in _compensations)
        {
            try
            {
                await compensation(ct);
            }
            catch (Exception compEx)
            {
                throw new SagaCompensationException(
                    step,
                    compensation.Method.Name,
                    lastException ?? compEx,
                    $"Compensation for step '{step}' failed: {compEx.Message}");
            }
        }
    }
}
