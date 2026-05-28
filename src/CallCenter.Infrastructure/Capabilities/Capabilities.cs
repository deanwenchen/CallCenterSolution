using CallCenter.Application;
using CallCenter.Domain;

namespace CallCenter.Infrastructure;

/// <summary>
/// 基于依赖注入集合构建的业务能力注册表。
/// </summary>
public sealed class CapabilityRegistry(IEnumerable<ICapability> capabilities) : ICapabilityRegistry
{
    private readonly Dictionary<string, ICapability> _capabilities =
        capabilities.ToDictionary(capability => capability.Key, StringComparer.OrdinalIgnoreCase);

    public ICapability Resolve(CapabilitySelection capability)
    {
        if (_capabilities.TryGetValue(capability.Key, out ICapability? resolved))
        {
            return resolved;
        }

        return _capabilities[CapabilityType.HumanAgent.ToString()];
    }

    /// <summary>
    /// 按能力类型解析业务能力，未注册时回退到人工能力。
    /// </summary>
    /// <param name="capability">业务能力类型。</param>
    /// <returns>业务能力实现。</returns>
    public ICapability Resolve(CapabilityType capability)
    {
        if (_capabilities.TryGetValue(capability.ToString(), out ICapability? resolved))
        {
            return resolved;
        }

        return _capabilities[CapabilityType.HumanAgent.ToString()];
    }
}

/// <summary>
/// 固定映射到单个 Workflow 的业务能力基类。
/// </summary>
public abstract class StaticWorkflowCapability : ICapability
{
    private readonly IEnumerable<ICapabilityWorkflowRouteProvider> _routeProviders;
    private readonly string _defaultWorkflowName;

    protected StaticWorkflowCapability(CapabilityType type, string workflowName, IEnumerable<ICapabilityWorkflowRouteProvider>? routeProviders = null)
    {
        Type = type;
        _defaultWorkflowName = workflowName;
        _routeProviders = routeProviders ?? [];
    }

    public string Key => Type.ToString();

    /// <summary>
    /// 业务能力类型。
    /// </summary>
    public CapabilityType Type { get; }

    /// <summary>
    /// 返回构造函数中配置的固定 Workflow。
    /// </summary>
    /// <param name="intent">意图识别结果。</param>
    /// <param name="capability">Planner 选择的能力。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Workflow 选择结果。</returns>
    public async Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        CapabilityWorkflowRoute? route = null;
        foreach (ICapabilityWorkflowRouteProvider routeProvider in _routeProviders)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await routeProvider.GetRoutesAsync(cancellationToken)
                .ConfigureAwait(false);

            route = routes.LastOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase)) ?? route;
        }

        string selectedWorkflow = route?.WorkflowName ?? _defaultWorkflowName;
        string reason = route?.Reason ?? $"Capability {Type} selected {selectedWorkflow}.";
        return new WorkflowSelection(selectedWorkflow, Type, reason, Key);
    }
}
