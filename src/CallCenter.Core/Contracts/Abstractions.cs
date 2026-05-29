
namespace CallCenter.Core;

/// <summary>
/// 会话入口编排接口，负责从用户消息驱动完整客服处理链路。
/// </summary>
public interface IConversationGateway
{
    /// <summary>
    /// 处理一轮用户消息，并返回 Workflow 执行后的会话响应。
    /// </summary>
    /// <param name="request">会话消息请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话响应。</returns>
    Task<ConversationResponse> HandleAsync(ConversationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 根据入口请求创建内部会话上下文。
/// </summary>
public interface IConversationContextFactory
{
    /// <summary>
    /// 将 API 请求转换为内部会话上下文。
    /// </summary>
    /// <param name="request">会话消息请求。</param>
    /// <returns>内部会话上下文。</returns>
    SessionContext Create(ConversationRequest request);
}

/// <summary>
/// 入口认证服务。
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 对会话消息请求进行认证。
    /// </summary>
    /// <param name="request">会话消息请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>认证结果。</returns>
    Task<AccessDecision> AuthenticateAsync(ConversationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 入口鉴权服务。
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// 判断当前会话是否有权限执行本轮请求。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="request">会话消息请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>鉴权结果。</returns>
    Task<AccessDecision> AuthorizeAsync(SessionContext session, ConversationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 会话入口限流服务。
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// 检查当前会话是否超过限流策略。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>限流检查结果。</returns>
    Task<RateLimitDecision> CheckAsync(SessionContext session, CancellationToken cancellationToken = default);
}

/// <summary>
/// 黑名单检查服务。
/// </summary>
public interface IBlacklistService
{
    /// <summary>
    /// 检查当前会话或用户是否在黑名单中。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>黑名单检查结果。</returns>
    Task<AccessDecision> CheckAsync(SessionContext session, CancellationToken cancellationToken = default);
}

/// <summary>
/// 审计事件写入接口。
/// </summary>
public interface IAuditSink
{
    /// <summary>
    /// 写入一条审计事件。
    /// </summary>
    /// <param name="auditEvent">审计事件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// 观测事件写入接口。
/// </summary>
public interface IObservabilitySink
{
    /// <summary>
    /// 记录一条观测事件。
    /// </summary>
    /// <param name="name">事件名称。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="properties">事件属性。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task TrackAsync(string name, SessionContext session, Dictionary<string, string> properties, CancellationToken cancellationToken = default);
}

/// <summary>
/// 活动 Workflow 会话状态存储。
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// 获取会话当前活动 Workflow 状态。
    /// </summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>活动 Workflow 状态；不存在时返回 null。</returns>
    Task<WorkflowState?> GetActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存会话当前活动 Workflow 状态。
    /// </summary>
    /// <param name="state">Workflow 状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task SaveAsync(WorkflowState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理会话当前活动 Workflow 状态。
    /// </summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task ClearActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 意图识别接口，只负责分类和实体抽取，不执行业务动作。
/// </summary>
public interface IIntentRecognizer
{
    /// <summary>
    /// 识别用户消息的意图和实体。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="message">用户消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>意图识别结果。</returns>
    Task<IntentResult> RecognizeAsync(SessionContext session, string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// 大模型客户端抽象，只负责文本推理，不绑定具体供应商。
/// </summary>
public interface IModelClient
{
    /// <summary>
    /// 根据消息列表调用模型并返回文本结果。
    /// </summary>
    /// <param name="request">模型调用请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模型调用结果。</returns>
    Task<ModelChatResponse> CompleteAsync(ModelChatRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 任务规划接口，负责从意图映射到业务能力。
/// </summary>
public interface IPlanner
{
    /// <summary>
    /// 根据意图和会话上下文选择业务能力。
    /// </summary>
    /// <param name="intent">意图识别结果。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务能力选择结果。</returns>
    Task<CapabilitySelection> SelectCapabilityAsync(IntentResult intent, SessionContext session, CancellationToken cancellationToken = default);
}

/// <summary>
/// 业务能力接口，负责策略判断和 Workflow 选择。
/// </summary>
public interface ICapability
{
    /// <summary>
    /// 能力标识。内置能力通常等于 Type，自定义能力可以用自己的字符串。
    /// </summary>
    string Key { get; }

    /// <summary>
    /// 业务能力类型。
    /// </summary>
    CapabilityType Type { get; }

    /// <summary>
    /// 根据意图、Planner 结果和会话上下文选择具体 Workflow。
    /// </summary>
    /// <param name="intent">意图识别结果。</param>
    /// <param name="capability">Planner 选择的能力。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Workflow 选择结果。</returns>
    Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 业务能力注册表。
/// </summary>
public interface ICapabilityRegistry
{
    /// <summary>
    /// 按能力选择结果解析业务能力。
    /// </summary>
    ICapability Resolve(CapabilitySelection capability);

    /// <summary>
    /// 按能力类型解析业务能力实现。
    /// </summary>
    /// <param name="capability">业务能力类型。</param>
    /// <returns>业务能力实现。</returns>
    ICapability Resolve(CapabilityType capability);
}

/// <summary>
/// Workflow 定义注册表。
/// </summary>
public interface IWorkflowDefinitionRegistry
{
    /// <summary>
    /// 获取指定 Workflow 定义。
    /// </summary>
    /// <param name="workflowName">Workflow 名称。</param>
    /// <returns>Workflow 定义。</returns>
    WorkflowDefinition Get(string workflowName);

    /// <summary>
    /// 获取所有已注册 Workflow 定义。
    /// </summary>
    /// <returns>Workflow 定义集合。</returns>
    IReadOnlyCollection<WorkflowDefinition> GetAll();
}

/// <summary>
/// 流程定义提供者。每个业务模块可以单独提供自己的 WorkflowDefinition。
/// </summary>
public interface IWorkflowDefinitionProvider
{
    /// <summary>
    /// 返回该模块提供的流程定义。
    /// </summary>
    IReadOnlyCollection<WorkflowDefinition> GetDefinitions();
}

/// <summary>
/// Workflow Runtime 接口，封装流程启动和恢复。
/// </summary>
public interface IWorkflowRuntime
{
    /// <summary>
    /// 从首个 Step 启动一个 Workflow。
    /// </summary>
    /// <param name="workflow">Workflow 选择结果。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="message">当前用户消息。</param>
    /// <param name="entities">意图层抽取出的实体。</param>
    /// <param name="data">上游传入的初始数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Workflow 执行结果。</returns>
    Task<WorkflowExecutionResult> RunAsync(
        WorkflowSelection workflow,
        SessionContext session,
        string message,
        Dictionary<string, string> entities,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 从持久化状态恢复一个活动 Workflow。
    /// </summary>
    /// <param name="state">已持久化的 Workflow 状态。</param>
    /// <param name="session">当前会话上下文。</param>
    /// <param name="message">当前用户消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Workflow 执行结果。</returns>
    Task<WorkflowExecutionResult> ResumeAsync(
        WorkflowState state,
        SessionContext session,
        string message,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Workflow Step 调用的原子业务动作接口。
/// </summary>
public interface IBusinessAction
{
    /// <summary>
    /// 业务动作名称，需要与 WorkflowStepDefinition.BusinessActionName 对应。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 执行业务动作。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作执行结果。</returns>
    Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// 业务动作注册表。
/// </summary>
public interface IBusinessActionRegistry
{
    /// <summary>
    /// 按名称解析业务动作实现。
    /// </summary>
    /// <param name="businessActionName">业务动作名称。</param>
    /// <returns>业务动作实现。</returns>
    IBusinessAction Resolve(string businessActionName);
}

/// <summary>
/// 外部系统统一网关接口，生产环境应映射到真实 MCP 工具调用。
/// </summary>
public interface IExternalSystemGateway
{
    /// <summary>
    /// 调用外部系统并返回强类型结果。
    /// </summary>
    /// <typeparam name="TRequest">请求体类型。</typeparam>
    /// <typeparam name="TResponse">响应体类型。</typeparam>
    /// <param name="call">外部系统调用描述。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>外部系统响应。</returns>
    Task<TResponse> InvokeAsync<TRequest, TResponse>(
        ExternalSystemCall<TRequest> call,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 知识检索服务接口。
/// </summary>
public interface IKnowledgeService
{
    /// <summary>
    /// 根据会话消息和实体搜索知识库。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="query">检索文本。</param>
    /// <param name="entities">已抽取实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>知识检索结果。</returns>
    Task<KnowledgeSearchResult> SearchAsync(
        SessionContext session,
        string query,
        Dictionary<string, string> entities,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 人工客服服务接口。
/// </summary>
public interface IHumanAgentService
{
    /// <summary>
    /// 创建人工客服工单。
    /// </summary>
    /// <param name="session">会话上下文。</param>
    /// <param name="reason">转人工原因。</param>
    /// <param name="data">当前业务数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>人工客服工单。</returns>
    Task<HumanAgentTicket> CreateTicketAsync(
        SessionContext session,
        string reason,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 意图配置来源。
/// </summary>
public interface IIntentDefinitionProvider
{
    Task<IReadOnlyCollection<IntentDefinition>> GetIntentDefinitionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 意图到能力的路由配置来源。
/// </summary>
public interface IIntentCapabilityRouteProvider
{
    Task<IReadOnlyCollection<IntentCapabilityRoute>> GetRoutesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 能力到 Workflow 的路由配置来源。
/// </summary>
public interface ICapabilityWorkflowRouteProvider
{
    Task<IReadOnlyCollection<CapabilityWorkflowRoute>> GetRoutesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Workflow 可用动作和工具的配置来源。
/// </summary>
public interface IWorkflowPermissionProvider
{
    Task<WorkflowPermissionDefinition?> GetPermissionAsync(string workflowName, CancellationToken cancellationToken = default);
}
