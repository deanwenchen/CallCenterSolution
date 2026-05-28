using CallCenter.Domain;

namespace CallCenter.Application;

/// <summary>
/// 会话网关实现，负责入口治理、会话状态路由以及 Workflow 启动/恢复。
/// </summary>
public sealed class ConversationGateway(
    IConversationContextFactory contextFactory,
    IAuthenticationService authenticationService,
    IAuthorizationService authorizationService,
    IRateLimiter rateLimiter,
    IBlacklistService blacklistService,
    IAuditSink auditSink,
    IObservabilitySink observabilitySink,
    ISessionStore sessionStore,
    IIntentRecognizer intentRecognizer,
    IPlanner planner,
    ICapabilityRegistry capabilityRegistry,
    IWorkflowRuntime workflowRuntime) : IConversationGateway
{
    /// <summary>
    /// 处理一轮会话消息；有活动 Workflow 时直接恢复，否则执行意图识别、规划和 Workflow 选择。
    /// </summary>
    /// <param name="request">会话消息请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话响应。</returns>
    public async Task<ConversationResponse> HandleAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        SessionContext session = contextFactory.Create(request);
        await AuditAsync("conversation.received", session, request, [], cancellationToken).ConfigureAwait(false);

        ConversationResponse? rejected = await RunGatewayChecksAsync(session, request, cancellationToken).ConfigureAwait(false);
        if (rejected is not null)
        {
            await AuditAsync("conversation.rejected", session, request, rejected.Data, cancellationToken).ConfigureAwait(false);
            return rejected;
        }

        WorkflowState? activeWorkflow = await sessionStore.GetActiveWorkflowAsync(session.SessionId, cancellationToken)
            .ConfigureAwait(false);

        WorkflowExecutionResult result;
        if (activeWorkflow is not null && activeWorkflow.Status is WorkflowStatus.Running or WorkflowStatus.WaitingForHuman)
        {
            await observabilitySink.TrackAsync(
                    "conversation.route.active_workflow",
                    session,
                    new Dictionary<string, string> { ["workflow"] = activeWorkflow.WorkflowName, ["step"] = activeWorkflow.CurrentStep ?? string.Empty },
                    cancellationToken)
                .ConfigureAwait(false);

            result = await workflowRuntime.ResumeAsync(activeWorkflow, session, request.Message, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            IntentResult intent = await intentRecognizer.RecognizeAsync(session, request.Message, cancellationToken)
                .ConfigureAwait(false);
            CapabilitySelection capability = await planner.SelectCapabilityAsync(intent, session, cancellationToken)
                .ConfigureAwait(false);
            WorkflowSelection workflow = await capabilityRegistry.Resolve(capability)
                .SelectWorkflowAsync(intent, capability, session, cancellationToken)
                .ConfigureAwait(false);

            await observabilitySink.TrackAsync(
                    "conversation.route.new_workflow",
                    session,
                    new Dictionary<string, string>
                    {
                        ["intent"] = intent.Intent.ToString(),
                        ["capability"] = capability.Capability.ToString(),
                        ["workflow"] = workflow.WorkflowName
                    },
                    cancellationToken)
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

        var response = new ConversationResponse(
            session.SessionId,
            result.WorkflowInstanceId,
            result.Status,
            result.Message ?? string.Empty,
            result.WorkflowName,
            result.CurrentStep,
            result.Data);

        await AuditAsync(
                "conversation.completed",
                session,
                request,
                new Dictionary<string, string>
                {
                    ["workflow"] = response.WorkflowName ?? string.Empty,
                    ["step"] = response.CurrentStep ?? string.Empty,
                    ["status"] = response.Status.ToString()
                },
                cancellationToken)
            .ConfigureAwait(false);

        return response;
    }

    private async Task<ConversationResponse?> RunGatewayChecksAsync(
        SessionContext session,
        ConversationRequest request,
        CancellationToken cancellationToken)
    {
        AccessDecision auth = await authenticationService.AuthenticateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!auth.Allowed)
        {
            return Reject(session, "AUTH_REJECTED", auth.Reason);
        }

        AccessDecision blacklist = await blacklistService.CheckAsync(session, cancellationToken).ConfigureAwait(false);
        if (!blacklist.Allowed)
        {
            return Reject(session, "BLACKLISTED", blacklist.Reason);
        }

        AccessDecision authorization = await authorizationService.AuthorizeAsync(session, request, cancellationToken).ConfigureAwait(false);
        if (!authorization.Allowed)
        {
            return Reject(session, "FORBIDDEN", authorization.Reason);
        }

        RateLimitDecision rateLimit = await rateLimiter.CheckAsync(session, cancellationToken).ConfigureAwait(false);
        if (!rateLimit.Allowed)
        {
            return Reject(
                session,
                "RATE_LIMITED",
                rateLimit.Reason,
                new Dictionary<string, string>
                {
                    ["limit"] = rateLimit.Limit.ToString(),
                    ["remaining"] = rateLimit.Remaining.ToString(),
                    ["retryAfterSeconds"] = ((int)Math.Ceiling(rateLimit.RetryAfter.TotalSeconds)).ToString()
                });
        }

        return null;
    }

    private static ConversationResponse Reject(
        SessionContext session,
        string code,
        string reason,
        Dictionary<string, string>? data = null)
    {
        Dictionary<string, string> responseData = data is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);

        responseData["code"] = code;
        responseData["reason"] = reason;

        return new ConversationResponse(
            session.SessionId,
            $"{session.SessionId}:rejected",
            WorkflowStatus.Failed,
            reason,
            null,
            null,
            responseData);
    }

    private Task AuditAsync(
        string name,
        SessionContext session,
        ConversationRequest request,
        Dictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> auditData = new(data, StringComparer.OrdinalIgnoreCase)
        {
            ["channel"] = request.Channel,
            ["messageLength"] = request.Message.Length.ToString()
        };

        return auditSink.WriteAsync(
            new AuditEvent(
                name,
                session.SessionId,
                session.UserId,
                session.TenantId,
                session.CorrelationId,
                DateTimeOffset.UtcNow,
                auditData),
            cancellationToken);
    }
}
