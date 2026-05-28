using CallCenter.Domain;

namespace CallCenter.Application;

public sealed class ConversationGateway(
    IConversationContextFactory contextFactory,
    ISessionStore sessionStore,
    IIntentRecognizer intentRecognizer,
    IPlanner planner,
    ICapabilityRegistry capabilityRegistry,
    IWorkflowRuntime workflowRuntime) : IConversationGateway
{
    public async Task<ConversationResponse> HandleAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        SessionContext session = contextFactory.Create(request);
        WorkflowState? activeWorkflow = await sessionStore.GetActiveWorkflowAsync(session.SessionId, cancellationToken)
            .ConfigureAwait(false);

        WorkflowExecutionResult result;
        if (activeWorkflow is not null && activeWorkflow.Status is WorkflowStatus.Running or WorkflowStatus.WaitingForHuman)
        {
            // 已有活动流程时跳过 Intent/Planner，直接回到 Workflow Engine 续跑当前 Step。
            result = await workflowRuntime.ResumeAsync(activeWorkflow, session, request.Message, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            // 新会话路径：Intent 只识别，Planner 只选 Capability，Capability 再选择 Workflow。
            IntentResult intent = await intentRecognizer.RecognizeAsync(session, request.Message, cancellationToken)
                .ConfigureAwait(false);
            CapabilitySelection capability = await planner.SelectCapabilityAsync(intent, session, cancellationToken)
                .ConfigureAwait(false);
            WorkflowSelection workflow = await capabilityRegistry.Resolve(capability.Capability)
                .SelectWorkflowAsync(intent, capability, session, cancellationToken)
                .ConfigureAwait(false);

            result = await workflowRuntime.RunAsync(
                    workflow,
                    session,
                    request.Message,
                    intent.Entities,
                    request.Metadata,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var state = new WorkflowState(
            result.WorkflowInstanceId,
            session.SessionId,
            result.WorkflowName,
            result.Status,
            result.CurrentStep,
            result.CheckpointSessionId,
            result.CheckpointId,
            result.Data);

        if (result.Status is WorkflowStatus.Completed or WorkflowStatus.Failed)
        {
            await sessionStore.ClearActiveWorkflowAsync(session.SessionId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await sessionStore.SaveAsync(state, cancellationToken).ConfigureAwait(false);
        }

        return new ConversationResponse(
            session.SessionId,
            result.WorkflowInstanceId,
            result.Status,
            result.Message ?? string.Empty,
            result.WorkflowName,
            result.CurrentStep,
            result.Data);
    }
}
