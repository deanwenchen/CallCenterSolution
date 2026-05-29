namespace CallCenter.Core;

/// <summary>
/// 用户消息经过意图识别后得到的标准意图类型。
/// </summary>
public enum IntentType
{
    /// <summary>无法识别或置信度不足的意图。</summary>
    Unknown,
    /// <summary>退款、退货或售后退款相关意图。</summary>
    Refund,
    /// <summary>物流、快递、包裹跟踪相关意图。</summary>
    Logistics,
    /// <summary>发票开具或发票查询相关意图。</summary>
    Invoice,
    /// <summary>CRM 标签、客户分层或客户跟进相关意图。</summary>
    Crm,
    /// <summary>订阅、续费、取消订阅相关意图。</summary>
    Subscribe,
    /// <summary>会员等级、积分、权益相关意图。</summary>
    Member,
    /// <summary>优惠券、折扣券、抵扣券相关意图。</summary>
    Coupon,
    /// <summary>商品退货相关意图。</summary>
    ProductReturn,
    /// <summary>明确需要人工客服或模型兜底的意图。</summary>
    HumanAgent
}

/// <summary>
/// Planner 选择出的业务能力类型，Capability 会进一步选择具体 Workflow。
/// </summary>
public enum CapabilityType
{
    /// <summary>未选择到可用能力。</summary>
    Unknown,
    /// <summary>退款能力。</summary>
    Refund,
    /// <summary>物流能力。</summary>
    Logistics,
    /// <summary>发票能力。</summary>
    Invoice,
    /// <summary>CRM 能力。</summary>
    Crm,
    /// <summary>订阅能力。</summary>
    Subscribe,
    /// <summary>会员能力。</summary>
    Member,
    /// <summary>优惠券能力。</summary>
    Coupon,
    /// <summary>商品退货能力。</summary>
    ProductReturn,
    /// <summary>人工客服能力。</summary>
    HumanAgent
}

/// <summary>
/// 会话当前关联 Workflow 的生命周期状态。
/// </summary>
public enum WorkflowStatus
{
    /// <summary>流程尚未开始。</summary>
    NotStarted,
    /// <summary>流程正在执行。</summary>
    Running,
    /// <summary>流程暂停，正在等待用户、人工客服或人工审批输入。</summary>
    WaitingForHuman,
    /// <summary>流程已正常完成。</summary>
    Completed,
    /// <summary>流程执行失败。</summary>
    Failed
}

/// <summary>
/// 单个业务动作在 Workflow Step 中的执行结果状态。
/// </summary>
public enum BusinessActionExecutionStatus
{
    /// <summary>业务动作执行成功。</summary>
    Succeeded,
    /// <summary>业务动作执行失败。</summary>
    Failed,
    /// <summary>业务动作需要等待用户或人工输入。</summary>
    AwaitingHumanInput,
    /// <summary>业务动作被跳过。</summary>
    Skipped
}

/// <summary>
/// 入口网关进行认证、鉴权、黑名单等检查后的准入状态。
/// </summary>
public enum AccessDecisionStatus
{
    /// <summary>允许继续处理。</summary>
    Allowed,
    /// <summary>拒绝继续处理。</summary>
    Denied
}

/// <summary>
/// 业务风险等级，用于 Capability 或 Workflow 决策。
/// </summary>
public enum RiskLevel
{
    /// <summary>低风险。</summary>
    Low,
    /// <summary>中风险。</summary>
    Medium,
    /// <summary>高风险。</summary>
    High
}

/// <summary>
/// 一轮对话请求在系统内部传递的会话上下文。
/// </summary>
/// <param name="SessionId">会话标识，用于查找活动 Workflow 状态。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Channel">接入渠道，例如 web、app、wecom。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="IsAuthenticated">当前请求是否已通过认证。</param>
/// <param name="CorrelationId">链路追踪标识。</param>
/// <param name="Attributes">网关或上游传入的上下文属性。</param>
public sealed record SessionContext(
    string SessionId,
    string UserId,
    string Channel,
    string TenantId,
    bool IsAuthenticated,
    string CorrelationId,
    Dictionary<string, string>? Attributes = null);

/// <summary>
/// 意图识别层输出的标准结果。
/// </summary>
/// <param name="Intent">识别出的意图。</param>
/// <param name="Confidence">置信度，取值范围由实现约定。</param>
/// <param name="Entities">从用户消息中抽取出的实体。</param>
public sealed record IntentResult(
    IntentType Intent,
    double Confidence,
    Dictionary<string, string> Entities,
    string? IntentKey = null)
{
    public string Key => IntentKey ?? Intent.ToString();
}

/// <summary>
/// Planner 选择业务能力后的结果。
/// </summary>
/// <param name="Capability">被选中的业务能力。</param>
/// <param name="Reason">选择该能力的原因，主要用于审计和排查。</param>
public sealed record CapabilitySelection(
    CapabilityType Capability,
    string Reason,
    string? CapabilityKey = null)
{
    public string Key => CapabilityKey ?? Capability.ToString();
}

/// <summary>
/// Capability 根据业务策略选择具体 Workflow 后的结果。
/// </summary>
/// <param name="WorkflowName">要执行的 Workflow 名称。</param>
/// <param name="Capability">该 Workflow 所属能力。</param>
/// <param name="Reason">选择该 Workflow 的原因。</param>
public sealed record WorkflowSelection(
    string WorkflowName,
    CapabilityType Capability,
    string Reason,
    string? CapabilityKey = null)
{
    public string Key => CapabilityKey ?? Capability.ToString();
}

/// <summary>
/// Workflow 的静态编排定义。
/// </summary>
/// <param name="Name">Workflow 唯一名称。</param>
/// <param name="Capability">Workflow 所属业务能力。</param>
/// <param name="Steps">Workflow 中包含的 Step 定义。</param>
/// <param name="Edges">Step 之间的流转边定义。</param>
/// <param name="StateModel">状态模型名称，用于标识该流程使用的持久化状态结构。</param>
/// <param name="CapabilityKey">可选能力标识，用于自定义能力。</param>
public sealed record WorkflowDefinition(
    string Name,
    CapabilityType Capability,
    IReadOnlyList<WorkflowStepDefinition> Steps,
    IReadOnlyList<WorkflowEdgeDefinition> Edges,
    string StateModel,
    string? CapabilityKey = null)
{
    public string DisplayCapability => CapabilityKey ?? Capability.ToString();
}

/// <summary>
/// Workflow 中单个 Step 的静态定义。
/// </summary>
/// <param name="Name">Step 名称，通常使用大写业务阶段名。</param>
/// <param name="BusinessActionName">该 Step 要调用的业务动作名称。</param>
/// <param name="Timeout">单次业务动作执行超时时间。</param>
/// <param name="MaxRetries">业务动作失败后的最大重试次数。</param>
/// <param name="CompensationBusinessActionName">失败时可选执行的补偿业务动作名称。</param>
/// <param name="HumanInTheLoop">该 Step 是否属于人机协同或人工介入节点。</param>
public sealed record WorkflowStepDefinition(
    string Name,
    string BusinessActionName,
    TimeSpan Timeout,
    int MaxRetries,
    string? CompensationBusinessActionName = null,
    bool HumanInTheLoop = false);

/// <summary>
/// Workflow 中两个 Step 之间的有向边。
/// </summary>
/// <param name="FromStep">起始 Step 名称。</param>
/// <param name="ToStep">目标 Step 名称。</param>
/// <param name="Condition">边条件表达式描述，当前主要用于审计和可读性。</param>
public sealed record WorkflowEdgeDefinition(
    string FromStep,
    string ToStep,
    string? Condition = null);

/// <summary>
/// 会话中活动 Workflow 的可持久化状态。
/// </summary>
/// <param name="WorkflowInstanceId">Workflow 实例标识。</param>
/// <param name="SessionId">所属会话标识。</param>
/// <param name="WorkflowName">Workflow 名称。</param>
/// <param name="Status">Workflow 当前状态。</param>
/// <param name="CurrentStep">当前停留或最后完成的 Step。</param>
/// <param name="CheckpointSessionId">Agent Framework checkpoint session 标识。</param>
/// <param name="CheckpointId">Agent Framework checkpoint 标识。</param>
/// <param name="Data">跨轮会话和跨 Step 传递的业务数据。</param>
public sealed record WorkflowState(
    string WorkflowInstanceId,
    string SessionId,
    string WorkflowName,
    WorkflowStatus Status,
    string? CurrentStep,
    string? CheckpointSessionId,
    string? CheckpointId,
    Dictionary<string, string> Data);

/// <summary>
/// 客服会话消息 API 的请求模型。
/// </summary>
/// <param name="SessionId">会话标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Channel">接入渠道。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="Message">用户输入的原始消息。</param>
/// <param name="AuthToken">认证令牌，当前本地实现只做占位校验。</param>
/// <param name="Metadata">上游传入的元数据。</param>
public sealed record ConversationRequest(
    string SessionId,
    string UserId,
    string Channel,
    string TenantId,
    string Message,
    string? AuthToken = null,
    Dictionary<string, string>? Metadata = null);

/// <summary>
/// 客服会话消息 API 的响应模型。
/// </summary>
/// <param name="SessionId">会话标识。</param>
/// <param name="WorkflowInstanceId">本轮命中的 Workflow 实例标识。</param>
/// <param name="Status">Workflow 执行后的状态。</param>
/// <param name="Message">返回给用户或调用方的消息。</param>
/// <param name="WorkflowName">Workflow 名称。</param>
/// <param name="CurrentStep">当前 Step。</param>
/// <param name="Data">返回的业务数据。</param>
public sealed record ConversationResponse(
    string SessionId,
    string WorkflowInstanceId,
    WorkflowStatus Status,
    string Message,
    string? WorkflowName,
    string? CurrentStep,
    Dictionary<string, string> Data);

/// <summary>
/// 传入 Workflow Runtime 的执行请求。
/// </summary>
/// <param name="Session">会话上下文。</param>
/// <param name="WorkflowName">Workflow 名称。</param>
/// <param name="Message">当前轮用户消息。</param>
/// <param name="Entities">意图层抽取的实体。</param>
/// <param name="Data">上游或上一次 Step 累积的数据。</param>
/// <param name="StartStep">可选起始 Step，用于恢复流程。</param>
public sealed record WorkflowExecutionRequest(
    SessionContext Session,
    string WorkflowName,
    string Message,
    Dictionary<string, string> Entities,
    Dictionary<string, string> Data,
    string? StartStep = null);

/// <summary>
/// Workflow Runtime 执行或恢复后的统一结果。
/// </summary>
/// <param name="WorkflowInstanceId">Workflow 实例标识。</param>
/// <param name="WorkflowName">Workflow 名称。</param>
/// <param name="Status">Workflow 状态。</param>
/// <param name="CurrentStep">当前 Step。</param>
/// <param name="Message">返回给会话层的消息。</param>
/// <param name="CheckpointSessionId">Agent Framework checkpoint session 标识。</param>
/// <param name="CheckpointId">Agent Framework checkpoint 标识。</param>
/// <param name="Data">Workflow 累积业务数据。</param>
public sealed record WorkflowExecutionResult(
    string WorkflowInstanceId,
    string WorkflowName,
    WorkflowStatus Status,
    string? CurrentStep,
    string? Message,
    string? CheckpointSessionId,
    string? CheckpointId,
    Dictionary<string, string> Data);

/// <summary>
/// 单个业务动作执行时收到的上下文。
/// </summary>
/// <param name="Session">会话上下文。</param>
/// <param name="WorkflowName">当前 Workflow 名称。</param>
/// <param name="StepName">当前 Step 名称。</param>
/// <param name="Message">当前轮用户消息。</param>
/// <param name="Data">当前业务数据。</param>
public sealed record BusinessActionContext(
    SessionContext Session,
    string WorkflowName,
    string StepName,
    string Message,
    Dictionary<string, string> Data);

/// <summary>
/// BusinessAction 在 Workflow Step 之间传递的统一结果。
/// </summary>
/// <param name="StepName">产出该结果的 Step 名称。</param>
/// <param name="Status">业务动作执行状态。</param>
/// <param name="UserMessage">返回给用户的消息。</param>
/// <param name="Data">业务动作产出的数据。</param>
/// <param name="Session">会话上下文，用于后续 Step 继续传递。</param>
/// <param name="WorkflowName">所属 Workflow 名称。</param>
/// <param name="RequiresHumanInput">是否需要等待用户或人工输入。</param>
public sealed record BusinessActionResult(
    string StepName,
    BusinessActionExecutionStatus Status,
    string? UserMessage,
    Dictionary<string, string> Data,
    SessionContext? Session = null,
    string? WorkflowName = null,
    bool RequiresHumanInput = false)
{
    /// <summary>
    /// 指示 Workflow 是否可以沿下一条边继续流转。
    /// </summary>
    public bool CanContinue => Status == BusinessActionExecutionStatus.Succeeded && !RequiresHumanInput;
}

/// <summary>
/// 外部系统调用的统一请求描述，由 Infrastructure 映射到本地模拟或真实 MCP。
/// </summary>
/// <typeparam name="TRequest">外部调用请求体类型。</typeparam>
/// <param name="SystemName">外部系统名称。</param>
/// <param name="OperationName">外部系统操作名称。</param>
/// <param name="Request">请求体。</param>
/// <param name="CorrelationId">链路追踪标识。</param>
public sealed record ExternalSystemCall<TRequest>(
    string SystemName,
    string OperationName,
    TRequest Request,
    string CorrelationId,
    string? WorkflowName = null);

/// <summary>
/// 入口准入检查的统一结果。
/// </summary>
/// <param name="Status">准入状态。</param>
/// <param name="Reason">准入或拒绝原因。</param>
/// <param name="Attributes">准入检查附带的上下文属性。</param>
public sealed record AccessDecision(
    AccessDecisionStatus Status,
    string Reason,
    Dictionary<string, string>? Attributes = null)
{
    /// <summary>
    /// 指示请求是否允许继续进入会话路由。
    /// </summary>
    public bool Allowed => Status == AccessDecisionStatus.Allowed;
}

/// <summary>
/// 限流检查结果。
/// </summary>
/// <param name="Allowed">是否允许继续处理。</param>
/// <param name="Limit">当前窗口内允许的最大请求数。</param>
/// <param name="Remaining">当前窗口剩余请求数。</param>
/// <param name="RetryAfter">被限流后建议重试等待时间。</param>
/// <param name="Reason">限流决策原因。</param>
public sealed record RateLimitDecision(
    bool Allowed,
    int Limit,
    int Remaining,
    TimeSpan RetryAfter,
    string Reason);

/// <summary>
/// 审计事件模型。
/// </summary>
/// <param name="Name">事件名称。</param>
/// <param name="SessionId">会话标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="TenantId">租户标识。</param>
/// <param name="CorrelationId">链路追踪标识。</param>
/// <param name="Timestamp">事件发生时间。</param>
/// <param name="Data">事件附加数据。</param>
public sealed record AuditEvent(
    string Name,
    string SessionId,
    string UserId,
    string TenantId,
    string CorrelationId,
    DateTimeOffset Timestamp,
    Dictionary<string, string> Data);

/// <summary>
/// 知识库条目。
/// </summary>
/// <param name="Id">知识条目标识。</param>
/// <param name="Title">知识标题。</param>
/// <param name="Content">知识正文。</param>
/// <param name="Tags">知识标签。</param>
public sealed record KnowledgeEntry(
    string Id,
    string Title,
    string Content,
    IReadOnlyCollection<string> Tags);

/// <summary>
/// 知识检索结果。
/// </summary>
/// <param name="Entries">命中的知识条目。</param>
/// <param name="Summary">面向业务动作或用户的摘要。</param>
public sealed record KnowledgeSearchResult(
    IReadOnlyCollection<KnowledgeEntry> Entries,
    string Summary);

/// <summary>
/// 人工客服工单。
/// </summary>
/// <param name="TicketId">工单标识。</param>
/// <param name="SessionId">会话标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Reason">转人工原因。</param>
/// <param name="Status">工单状态。</param>
public sealed record HumanAgentTicket(
    string TicketId,
    string SessionId,
    string UserId,
    string Reason,
    string Status);

/// <summary>
/// 订单系统返回的订单快照。
/// </summary>
/// <param name="OrderId">订单号。</param>
/// <param name="Amount">订单金额。</param>
/// <param name="Status">订单状态。</param>
/// <param name="Paid">是否已支付。</param>
/// <param name="CouponUsed">是否使用过优惠券。</param>
public sealed record OrderSnapshot(
    string OrderId,
    decimal Amount,
    string Status,
    bool Paid,
    bool CouponUsed);

/// <summary>
/// 退款规则判断结果。
/// </summary>
/// <param name="Approved">是否允许退款。</param>
/// <param name="RiskLevel">退款风险等级。</param>
/// <param name="Reason">规则判断原因。</param>
public sealed record RefundDecision(
    bool Approved,
    RiskLevel RiskLevel,
    string Reason);

/// <summary>
/// 退款系统返回的受理结果。
/// </summary>
/// <param name="RefundId">退款单号。</param>
/// <param name="OrderId">订单号。</param>
/// <param name="Amount">退款金额。</param>
/// <param name="Status">退款状态。</param>
public sealed record RefundReceipt(
    string RefundId,
    string OrderId,
    decimal Amount,
    string Status);

/// <summary>
/// 物流系统返回的物流快照。
/// </summary>
/// <param name="OrderId">订单号。</param>
/// <param name="Carrier">承运商。</param>
/// <param name="TrackingNo">物流单号。</param>
/// <param name="Status">物流状态。</param>
public sealed record LogisticsSnapshot(
    string OrderId,
    string Carrier,
    string TrackingNo,
    string Status);

/// <summary>
/// 通知系统返回的发送结果。
/// </summary>
/// <param name="MessageId">通知消息标识。</param>
/// <param name="Channel">发送渠道。</param>
/// <param name="Status">发送状态。</param>
public sealed record NotificationReceipt(
    string MessageId,
    string Channel,
    string Status);

/// <summary>
/// CRM 系统返回的标签处理结果。
/// </summary>
/// <param name="UserId">用户标识。</param>
/// <param name="Tag">标签名称。</param>
/// <param name="Status">处理状态。</param>
public sealed record CrmTagReceipt(
    string UserId,
    string Tag,
    string Status);

/// <summary>
/// 发票系统返回的开票结果。
/// </summary>
/// <param name="InvoiceId">发票标识。</param>
/// <param name="OrderId">订单号。</param>
/// <param name="Title">发票抬头。</param>
/// <param name="Status">开票状态。</param>
public sealed record InvoiceReceipt(
    string InvoiceId,
    string OrderId,
    string Title,
    string Status);

/// <summary>
/// 会员系统返回的会员快照。
/// </summary>
/// <param name="UserId">用户标识。</param>
/// <param name="Level">会员等级。</param>
/// <param name="Points">会员积分。</param>
/// <param name="Status">会员状态。</param>
public sealed record MemberSnapshot(
    string UserId,
    string Level,
    int Points,
    string Status);

/// <summary>
/// 优惠券系统返回的发券结果。
/// </summary>
/// <param name="CouponId">优惠券标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Status">发券状态。</param>
public sealed record CouponReceipt(
    string CouponId,
    string UserId,
    string Status);

/// <summary>
/// 订阅系统返回的订阅处理结果。
/// </summary>
/// <param name="SubscriptionId">订阅标识。</param>
/// <param name="UserId">用户标识。</param>
/// <param name="Status">订阅状态。</param>
public sealed record SubscriptionReceipt(
    string SubscriptionId,
    string UserId,
    string Status);
