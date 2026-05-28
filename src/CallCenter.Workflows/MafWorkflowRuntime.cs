using CallCenter.Application;
using CallCenter.Domain;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace CallCenter.Workflows;

public sealed class MafWorkflowRuntime : IWorkflowRuntime, IDisposable
{
    private readonly IWorkflowDefinitionRegistry _definitionRegistry;
    private readonly MafWorkflowFactory _workflowFactory;
    private readonly FileSystemJsonCheckpointStore _checkpointStore;
    private readonly CheckpointManager _checkpointManager;

    public MafWorkflowRuntime(IWorkflowDefinitionRegistry definitionRegistry, MafWorkflowFactory workflowFactory)
    {
        _definitionRegistry = definitionRegistry;
        _workflowFactory = workflowFactory;

        // Checkpoint 用于审计和后续增强为 MAF ExternalRequest 恢复；会话续跑由 WorkflowState 持久化驱动。
        var checkpointDirectory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "workflow-checkpoints"));
        _checkpointStore = new FileSystemJsonCheckpointStore(checkpointDirectory);
        _checkpointManager = CheckpointManager.CreateJson(_checkpointStore);
    }

    public Task<WorkflowExecutionResult> RunAsync(
        WorkflowSelection workflow,
        SessionContext session,
        string message,
        Dictionary<string, string> entities,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        WorkflowExecutionRequest request = new(
            session,
            workflow.WorkflowName,
            message,
            entities,
            data ?? [],
            StartStep: null);

        return ExecuteAsync(workflow.WorkflowName, session.SessionId, request, cancellationToken);
    }

    public Task<WorkflowExecutionResult> ResumeAsync(
        WorkflowState state,
        SessionContext session,
        string message,
        CancellationToken cancellationToken = default)
    {
        WorkflowExecutionRequest request = new(
            session,
            state.WorkflowName,
            message,
            [],
            state.Data,
            StartStep: state.CurrentStep);

        return ExecuteAsync(state.WorkflowName, state.SessionId, request, cancellationToken);
    }

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

        BusinessActionResult? lastBusinessActionResult = null;
        CheckpointInfo? lastCheckpoint = null;

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
                workflow,
                request,
                _checkpointManager,
                workflowInstanceId,
                cancellationToken)
            .ConfigureAwait(false);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (evt is ExecutorCompletedEvent { Data: BusinessActionResult businessActionResult })
            {
                lastBusinessActionResult = businessActionResult;
            }

            if (evt is WorkflowOutputEvent { Data: BusinessActionResult outputResult })
            {
                lastBusinessActionResult = outputResult;
            }

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
