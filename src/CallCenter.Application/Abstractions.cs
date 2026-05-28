using CallCenter.Domain;

namespace CallCenter.Application;

public interface IConversationGateway
{
    Task<ConversationResponse> HandleAsync(ConversationRequest request, CancellationToken cancellationToken = default);
}

public interface IConversationContextFactory
{
    SessionContext Create(ConversationRequest request);
}

public interface ISessionStore
{
    Task<WorkflowState?> GetActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default);

    Task SaveAsync(WorkflowState state, CancellationToken cancellationToken = default);

    Task ClearActiveWorkflowAsync(string sessionId, CancellationToken cancellationToken = default);
}

public interface IIntentRecognizer
{
    Task<IntentResult> RecognizeAsync(SessionContext session, string message, CancellationToken cancellationToken = default);
}

public interface IPlanner
{
    Task<CapabilitySelection> SelectCapabilityAsync(IntentResult intent, SessionContext session, CancellationToken cancellationToken = default);
}

public interface ICapability
{
    CapabilityType Type { get; }

    Task<WorkflowSelection> SelectWorkflowAsync(
        IntentResult intent,
        CapabilitySelection capability,
        SessionContext session,
        CancellationToken cancellationToken = default);
}

public interface ICapabilityRegistry
{
    ICapability Resolve(CapabilityType capability);
}

public interface IWorkflowDefinitionRegistry
{
    WorkflowDefinition Get(string workflowName);

    IReadOnlyCollection<WorkflowDefinition> GetAll();
}

public interface IWorkflowRuntime
{
    Task<WorkflowExecutionResult> RunAsync(
        WorkflowSelection workflow,
        SessionContext session,
        string message,
        Dictionary<string, string> entities,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    Task<WorkflowExecutionResult> ResumeAsync(
        WorkflowState state,
        SessionContext session,
        string message,
        CancellationToken cancellationToken = default);
}

public interface IBusinessAction
{
    string Name { get; }

    Task<BusinessActionResult> Execute(BusinessActionContext context, CancellationToken cancellationToken = default);
}

public interface IBusinessActionRegistry
{
    IBusinessAction Resolve(string businessActionName);
}

public interface IExternalSystemGateway
{
    Task<TResponse> InvokeAsync<TRequest, TResponse>(
        ExternalSystemCall<TRequest> call,
        CancellationToken cancellationToken = default);
}
