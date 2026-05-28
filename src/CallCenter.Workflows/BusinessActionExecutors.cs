using CallCenter.Application;
using CallCenter.Domain;
using Microsoft.Agents.AI.Workflows;
using static CallCenter.Workflows.BusinessActionExecutorSupport;

namespace CallCenter.Workflows;

internal sealed class StartBusinessActionExecutor(
    WorkflowStepDefinition step,
    IBusinessActionRegistry businessActionRegistry) : Executor<WorkflowExecutionRequest, BusinessActionResult>(step.Name, declareCrossRunShareable: false)
{
    public override async ValueTask<BusinessActionResult> HandleAsync(
        WorkflowExecutionRequest message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> data = new(message.Data, StringComparer.OrdinalIgnoreCase);
        foreach ((string key, string value) in message.Entities)
        {
            data[key] = value;
        }

        // MAF Executor 只做运行时适配：把 Workflow 消息转换为业务动作上下文。
        return await ExecuteBusinessActionAsync(step, businessActionRegistry, message.Session, message.WorkflowName, message.Message, data, cancellationToken)
            .ConfigureAwait(false);
    }
}

internal sealed class BusinessActionStepExecutor(
    WorkflowStepDefinition step,
    IBusinessActionRegistry businessActionRegistry) : Executor<BusinessActionResult, BusinessActionResult>(step.Name, declareCrossRunShareable: false)
{
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
        return await ExecuteBusinessActionAsync(step, businessActionRegistry, message.Session, message.WorkflowName, userMessage, message.Data, cancellationToken)
            .ConfigureAwait(false);
    }
}

internal static class BusinessActionExecutorSupport
{
    // retry、timeout、compensation 统一在 Workflow Step 内处理，业务动作不能绕过 Workflow 执行。
    public static async Task<BusinessActionResult> ExecuteBusinessActionAsync(
        WorkflowStepDefinition step,
        IBusinessActionRegistry businessActionRegistry,
        SessionContext session,
        string workflowName,
        string message,
        Dictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        data["lastUserMessage"] = message;
        IBusinessAction businessAction = businessActionRegistry.Resolve(step.BusinessActionName);
        var context = new BusinessActionContext(session, workflowName, step.Name, message, data);

        Exception? lastException = null;
        int attempts = Math.Max(1, step.MaxRetries + 1);
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
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
}
