using System;

namespace CallCenter.Framework.Saga;

public class SagaOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan[] RetryDelays { get; set; } = {
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30)
    };
    public bool EnableCompensation { get; set; } = true;
}
