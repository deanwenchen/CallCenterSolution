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
/// 退款能力，负责根据金额风险选择自动退款或人工退款流程。
/// </summary>
public sealed class RefundCapability : ICapability
{
    public string Key => CapabilityType.Refund.ToString();

    /// <summary>
    /// 业务能力类型。
    /// </summary>
    public CapabilityType Type => CapabilityType.Refund;

    /// <summary>
    /// 根据退款金额选择标准退款或人工退款 Workflow。
    /// </summary>
    /// <param name="intent">意图识别结果。</param>
    /// <param name="capability">Planner 选择的能力。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Workflow 选择结果。</returns>
    public Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        RiskLevel risk = intent.Entities.TryGetValue("amount", out string? amountText) &&
                         decimal.TryParse(amountText, out decimal amount) &&
                         amount >= 1000
            ? RiskLevel.High
            : RiskLevel.Low;

        string workflowName = risk == RiskLevel.High ? "ManualRefundWorkflow" : "RefundWorkflow";
        return Task.FromResult(new WorkflowSelection(workflowName, Type, $"Risk {risk} selected {workflowName}."));
    }
}

/// <summary>
/// 发票能力，固定选择发票 Workflow。
/// </summary>
public sealed class InvoiceCapability : StaticWorkflowCapability
{
    public InvoiceCapability(ICapabilityWorkflowRouteProvider routeProvider) : base(CapabilityType.Invoice, "InvoiceWorkflow", routeProvider)
    {
    }
}

/// <summary>
/// 物流能力，固定选择物流 Workflow。
/// </summary>
public sealed class LogisticsCapability : StaticWorkflowCapability
{
    public LogisticsCapability(ICapabilityWorkflowRouteProvider routeProvider) : base(CapabilityType.Logistics, "LogisticsWorkflow", routeProvider)
    {
    }
}

/// <summary>
/// CRM 能力，固定选择 CRM Workflow。
/// </summary>
public sealed class CrmCapability : StaticWorkflowCapability
{
    public CrmCapability(ICapabilityWorkflowRouteProvider routeProvider) : base(CapabilityType.Crm, "CrmWorkflow", routeProvider)
    {
    }
}

/// <summary>
/// 订阅能力，固定选择订阅 Workflow。
/// </summary>
public sealed class SubscribeCapability : StaticWorkflowCapability
{
    public SubscribeCapability(ICapabilityWorkflowRouteProvider routeProvider) : base(CapabilityType.Subscribe, "SubscribeWorkflow", routeProvider)
    {
    }
}

/// <summary>
/// 会员能力，固定选择会员 Workflow。
/// </summary>
public sealed class MemberCapability : StaticWorkflowCapability
{
    public MemberCapability(ICapabilityWorkflowRouteProvider routeProvider) : base(CapabilityType.Member, "MemberWorkflow", routeProvider)
    {
    }
}

/// <summary>
/// 优惠券能力，固定选择优惠券 Workflow。
/// </summary>
public sealed class CouponCapability : StaticWorkflowCapability
{
    public CouponCapability(ICapabilityWorkflowRouteProvider routeProvider) : base(CapabilityType.Coupon, "CouponWorkflow", routeProvider)
    {
    }
}

/// <summary>
/// 人工客服能力，固定选择人工接管 Workflow。
/// </summary>
public sealed class HumanAgentCapability : StaticWorkflowCapability
{
    public HumanAgentCapability(ICapabilityWorkflowRouteProvider routeProvider) : base(CapabilityType.HumanAgent, "HumanHandoffWorkflow", routeProvider)
    {
    }
}

/// <summary>
/// 固定映射到单个 Workflow 的业务能力基类。
/// </summary>
public abstract class StaticWorkflowCapability : ICapability
{
    private readonly ICapabilityWorkflowRouteProvider? _routeProvider;
    private readonly string _defaultWorkflowName;

    protected StaticWorkflowCapability(CapabilityType type, string workflowName, ICapabilityWorkflowRouteProvider? routeProvider = null)
    {
        Type = type;
        _defaultWorkflowName = workflowName;
        _routeProvider = routeProvider;
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
        if (_routeProvider is not null)
        {
            IReadOnlyCollection<CapabilityWorkflowRoute> routes = await _routeProvider.GetRoutesAsync(cancellationToken)
                .ConfigureAwait(false);

            route = routes.FirstOrDefault(item => string.Equals(item.CapabilityKey, Key, StringComparison.OrdinalIgnoreCase));
        }

        string selectedWorkflow = route?.WorkflowName ?? _defaultWorkflowName;
        string reason = route?.Reason ?? $"Capability {Type} selected {selectedWorkflow}.";
        return new WorkflowSelection(selectedWorkflow, Type, reason, Key);
    }
}
