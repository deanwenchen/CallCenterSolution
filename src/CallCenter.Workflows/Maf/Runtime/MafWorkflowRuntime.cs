using CallCenter.Core;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace CallCenter.Workflows;

/// <summary>
/// 基于 MAF InProcessExecution 的 Workflow Runtime 实现。
/// </summary>
public sealed class MafWorkflowRuntime : IWorkflowRuntime, IDisposable
{
    private readonly IWorkflowDefinitionRegistry _definitionRegistry;
    private readonly MafWorkflowFactory _workflowFactory;
    private readonly FileSystemJsonCheckpointStore _checkpointStore;
    private readonly CheckpointManager _checkpointManager;

    /// <summary>
    /// 初始化 MAF Workflow Runtime，并创建本地 checkpoint 管理器。
    /// </summary>
    /// <param name="definitionRegistry">Workflow 定义注册表。</param>
    /// <param name="workflowFactory">MAF Workflow 工厂。</param>
    public MafWorkflowRuntime(IWorkflowDefinitionRegistry definitionRegistry, MafWorkflowFactory workflowFactory)
    {
        _definitionRegistry = definitionRegistry;
        _workflowFactory = workflowFactory;

        // Checkpoint 用于审计和后续增强为 MAF ExternalRequest 恢复；会话续跑由 WorkflowState 持久化驱动。
        var checkpointDirectory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "workflow-checkpoints"));
        _checkpointStore = new FileSystemJsonCheckpointStore(checkpointDirectory);
        _checkpointManager = CheckpointManager.CreateJson(_checkpointStore);
    }

    /// <summary>
    /// 从首个 Step 启动一个新的 Workflow 实例。
    /// </summary>
    /// <param name="workflow">Workflow 选择结果。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="message">当前用户消息。</param>
    /// <param name="entities">意图层抽取实体。</param>
    /// <param name="data">初始业务数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Workflow 执行结果。</returns>
    public Task<WorkflowExecutionResult> RunAsync(
        WorkflowSelection workflow,
        SessionContext session,
        string message,
        Dictionary<string, string> entities,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        // 新流程从 Workflow 的第一个 Step 开始执行。
        // entities 来自 Intent 层，data 通常来自请求 Metadata 或上游透传字段。
        WorkflowExecutionRequest request = new(
            session,
            workflow.WorkflowName,
            message,
            entities,
            data ?? [],
            StartStep: null);

        return ExecuteAsync(workflow.WorkflowName, session.SessionId, request, cancellationToken);
    }

    /// <summary>
    /// 从持久化状态中的当前 Step 恢复 Workflow。
    /// </summary>
    /// <param name="state">已持久化的 Workflow 状态。</param>
    /// <param name="session">当前会话上下文。</param>
    /// <param name="message">当前用户消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Workflow 执行结果。</returns>
    public Task<WorkflowExecutionResult> ResumeAsync(
        WorkflowState state,
        SessionContext session,
        string message,
        CancellationToken cancellationToken = default)
    {
        // 续跑流程从持久化的 CurrentStep 开始。
        // 这里不再传 Intent Entities，避免第二轮用户回复污染原始业务实体。
        WorkflowExecutionRequest request = new(
            session,
            state.WorkflowName,
            message,
            [],
            state.Data,
            StartStep: state.CurrentStep);

        return ExecuteAsync(state.WorkflowName, state.SessionId, request, cancellationToken);
    }

    /// <summary>
    /// 释放 checkpoint store 资源。
    /// </summary>
    public void Dispose()
    {
        _checkpointStore.Dispose();
    }

    private async Task<WorkflowExecutionResult> ExecuteAsync(
        string workflowName,
        string sessionId,
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken)
    {
        WorkflowDefinition definition = _definitionRegistry.Get(workflowName);
        Workflow workflow = _workflowFactory.Create(definition, request.StartStep);
        string workflowInstanceId = $"{sessionId}:{workflowName}";

        // lastBusinessActionResult 保存最新完成 Step 的结果，用于推导最终会话状态。
        // lastCheckpoint 保存 MAF 超步检查点，便于后续审计或增强为真实检查点恢复。
        BusinessActionResult? lastBusinessActionResult = null;
        CheckpointInfo? lastCheckpoint = null;

        // 当前使用 MAF 的进程内执行模式。Workflow 构建、Executor 调度、边条件判断都由 MAF Runtime 完成。
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
                workflow,
                request,
                _checkpointManager,
                workflowInstanceId,
                cancellationToken)
            .ConfigureAwait(false);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            // ExecutorCompletedEvent 表示某个 BusinessAction Step 已完成。
            if (evt is ExecutorCompletedEvent { Data: BusinessActionResult businessActionResult })
            {
                lastBusinessActionResult = businessActionResult;
            }

            // WorkflowOutputEvent 表示 MAF Workflow 已产出最终输出。
            if (evt is WorkflowOutputEvent { Data: BusinessActionResult outputResult })
            {
                lastBusinessActionResult = outputResult;
            }

            // SuperStepCompletedEvent 携带 Checkpoint 信息，当前用于记录 MAF 运行轨迹。
            if (evt is SuperStepCompletedEvent { CompletionInfo.Checkpoint: { } checkpoint })
            {
                lastCheckpoint = checkpoint;
            }
        }

        if (lastBusinessActionResult is null)
        {
            return new WorkflowExecutionResult(
                workflowInstanceId,
                workflowName,
                WorkflowStatus.Failed,
                null,
                "Workflow completed without a business action result.",
                lastCheckpoint?.SessionId,
                lastCheckpoint?.CheckpointId,
                request.Data);
        }

        // 将 BusinessAction 的执行状态映射为对外会话状态。
        // AwaitingHumanInput 会返回给用户并保存 WorkflowState，下一轮消息再 Resume。
        WorkflowStatus status = lastBusinessActionResult.Status switch
        {
            BusinessActionExecutionStatus.AwaitingHumanInput => WorkflowStatus.WaitingForHuman,
            BusinessActionExecutionStatus.Failed => WorkflowStatus.Failed,
            _ => WorkflowStatus.Completed
        };

        return new WorkflowExecutionResult(
            workflowInstanceId,
            workflowName,
            status,
            lastBusinessActionResult.StepName,
            lastBusinessActionResult.UserMessage,
            lastCheckpoint?.SessionId,
            lastCheckpoint?.CheckpointId,
            lastBusinessActionResult.Data);
    }
}
