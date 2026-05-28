using CallCenter.Application;
using CallCenter.Domain;
using Microsoft.Agents.AI.Workflows;
using static CallCenter.Workflows.BusinessActionExecutorSupport;

namespace CallCenter.Workflows;

/// <summary>
/// MAF 首个 Step Executor，负责把 WorkflowExecutionRequest 转换为业务动作上下文。
/// </summary>
internal sealed class StartBusinessActionExecutor(
    WorkflowStepDefinition step,
    IBusinessActionRegistry businessActionRegistry,
    IWorkflowPermissionProvider permissionProvider) : Executor<WorkflowExecutionRequest, BusinessActionResult>(step.Name, declareCrossRunShareable: false)
{
    /// <summary>
    /// 执行 Workflow 的首个业务动作。
    /// </summary>
    /// <param name="message">Workflow 执行请求。</param>
    /// <param name="context">MAF Workflow 上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public override async ValueTask<BusinessActionResult> HandleAsync(
        WorkflowExecutionRequest message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // 首个 Step 需要合并入口 Metadata 和 Intent 抽取出的 Entities。
        // 后续 Step 只沿用 BusinessActionResult.Data，不再重新识别意图。
        Dictionary<string, string> data = new(message.Data, StringComparer.OrdinalIgnoreCase);
        foreach ((string key, string value) in message.Entities)
        {
            data[key] = value;
        }

        // MAF Executor 只做运行时适配：把 Workflow 消息转换为业务动作上下文。
        return await ExecuteBusinessActionAsync(step, businessActionRegistry, permissionProvider, message.Session, message.WorkflowName, message.Message, data, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// MAF 后续 Step Executor，负责把上一个业务动作结果传给下一个业务动作。
/// </summary>
internal sealed class BusinessActionStepExecutor(
    WorkflowStepDefinition step,
    IBusinessActionRegistry businessActionRegistry,
    IWorkflowPermissionProvider permissionProvider) : Executor<BusinessActionResult, BusinessActionResult>(step.Name, declareCrossRunShareable: false)
{
    /// <summary>
    /// 执行 Workflow 的后续业务动作。
    /// </summary>
    /// <param name="message">上一个 Step 的业务动作结果。</param>
    /// <param name="context">MAF Workflow 上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public override async ValueTask<BusinessActionResult> HandleAsync(
        BusinessActionResult message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (message.Session is null || message.WorkflowName is null)
        {
            throw new InvalidOperationException("Business action result is missing workflow execution context.");
        }

        string userMessage = message.Data.GetValueOrDefault("lastUserMessage") ?? string.Empty;
        // 后续 Step 保留上一轮用户消息，主要用于 WaitUserConfirm 等人工确认 Step 判断用户回复。
        return await ExecuteBusinessActionAsync(step, businessActionRegistry, permissionProvider, message.Session, message.WorkflowName, userMessage, message.Data, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// BusinessAction Executor 的公共支持方法。
/// </summary>
internal static class BusinessActionExecutorSupport
{
    // retry、timeout、compensation 统一在 Workflow Step 内处理，业务动作不能绕过 Workflow 执行。
    /// <summary>
    /// 解析并执行业务动作，同时统一处理 timeout、retry 和 compensation。
    /// </summary>
    /// <param name="step">当前 Step 定义。</param>
    /// <param name="businessActionRegistry">业务动作注册表。</param>
    /// <param name="session">会话上下文。</param>
    /// <param name="workflowName">Workflow 名称。</param>
    /// <param name="message">当前用户消息。</param>
    /// <param name="data">当前业务数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>业务动作结果。</returns>
    public static async Task<BusinessActionResult> ExecuteBusinessActionAsync(
        WorkflowStepDefinition step,
        IBusinessActionRegistry businessActionRegistry,
        IWorkflowPermissionProvider permissionProvider,
        SessionContext session,
        string workflowName,
        string message,
        Dictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        data["lastUserMessage"] = message;

        // 运行时通过注册表解析 BusinessAction，Workflow 定义只保存动作名称。
        // 这样可以保证业务动作只能被 Workflow Step 调用，而不会从入口层直接执行。
        await EnsureBusinessActionAllowedAsync(permissionProvider, workflowName, step.BusinessActionName, cancellationToken)
            .ConfigureAwait(false);

        IBusinessAction businessAction = businessActionRegistry.Resolve(step.BusinessActionName);
        var context = new BusinessActionContext(session, workflowName, step.Name, message, data);

        Exception? lastException = null;
        int attempts = Math.Max(1, step.MaxRetries + 1);
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            // 每次尝试都有独立超时控制；外部 cancellation 仍然优先传递。
            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(step.Timeout);

            try
            {
                BusinessActionResult result = await businessAction.Execute(context, timeoutSource.Token).ConfigureAwait(false);
                if (result.Status != BusinessActionExecutionStatus.Failed || attempt == attempts)
                {
                    return result;
                }
            }
            catch (Exception ex) when (attempt < attempts && ex is not OperationCanceledException)
            {
                lastException = ex;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < attempts)
            {
                lastException = ex;
            }
        }

        Dictionary<string, string> failedData = new(data, StringComparer.OrdinalIgnoreCase)
        {
            ["failedStep"] = step.Name,
            ["failureReason"] = lastException?.Message ?? "Business action returned failed status."
        };

        if (!string.IsNullOrWhiteSpace(step.CompensationBusinessActionName))
        {
            // 失败补偿只在 Step 配置了 CompensationBusinessActionName 时触发。
            // 当前退款流程用它表达“退款失败后尝试恢复优惠券”等兜底动作。
            await EnsureBusinessActionAllowedAsync(permissionProvider, workflowName, step.CompensationBusinessActionName, cancellationToken)
                .ConfigureAwait(false);
            IBusinessAction compensation = businessActionRegistry.Resolve(step.CompensationBusinessActionName);
            var compensationContext = new BusinessActionContext(session, workflowName, $"{step.Name}_COMPENSATE", message, failedData);
            BusinessActionResult compensationResult = await compensation.Execute(compensationContext, cancellationToken).ConfigureAwait(false);
            foreach ((string key, string value) in compensationResult.Data)
            {
                failedData[key] = value;
            }
        }

        return new BusinessActionResult(
            step.Name,
            BusinessActionExecutionStatus.Failed,
            $"步骤 {step.Name} 执行失败。",
            failedData,
            session,
            workflowName);
    }

    private static async Task EnsureBusinessActionAllowedAsync(
        IWorkflowPermissionProvider permissionProvider,
        string workflowName,
        string businessActionName,
        CancellationToken cancellationToken)
    {
        WorkflowPermissionDefinition? permission = await permissionProvider.GetPermissionAsync(workflowName, cancellationToken)
            .ConfigureAwait(false);

        if (permission is null)
        {
            return;
        }

        if (!permission.BusinessActions.Contains(businessActionName, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"BusinessAction '{businessActionName}' is not allowed in workflow '{workflowName}'.");
        }
    }
}
