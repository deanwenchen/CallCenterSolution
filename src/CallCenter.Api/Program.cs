using CallCenter.Application;
using CallCenter.Domain;
using CallCenter.Infrastructure;
using CallCenter.BusinessActions;
using CallCenter.Workflows;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddScoped<IConversationGateway, ConversationGateway>();

builder.Services.AddSingleton<ISessionStore, FileSessionStore>();
builder.Services.AddSingleton<IConversationContextFactory, ConversationContextFactory>();
builder.Services.AddSingleton<IIntentRecognizer, RuleBasedIntentRecognizer>();
builder.Services.AddSingleton<IPlanner, DefaultPlanner>();
builder.Services.AddSingleton<IExternalSystemGateway, InMemoryExternalSystemGateway>();

builder.Services.AddSingleton<ICapability, RefundCapability>();
builder.Services.AddSingleton<ICapability, LogisticsCapability>();
builder.Services.AddSingleton<ICapability, CrmCapability>();
builder.Services.AddSingleton<ICapability, HumanAgentCapability>();
builder.Services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();

builder.Services.AddSingleton<IBusinessAction, QueryOrderBusinessAction>();
builder.Services.AddSingleton<IBusinessAction, CheckRefundRuleBusinessAction>();
builder.Services.AddSingleton<IBusinessAction, WaitUserConfirmBusinessAction>();
builder.Services.AddSingleton<IBusinessAction, ExecuteRefundBusinessAction>();
builder.Services.AddSingleton<IBusinessAction, RestoreCouponBusinessAction>();
builder.Services.AddSingleton<IBusinessAction, SendNotificationBusinessAction>();
builder.Services.AddSingleton<IBusinessAction, QueryLogisticsBusinessAction>();
builder.Services.AddSingleton<IBusinessAction, AddCrmTagBusinessAction>();
builder.Services.AddSingleton<IBusinessAction, HumanHandoffBusinessAction>();
builder.Services.AddSingleton<IBusinessActionRegistry, BusinessActionRegistry>();

builder.Services.AddSingleton<IWorkflowDefinitionRegistry, WorkflowDefinitionRegistry>();
builder.Services.AddSingleton<MafWorkflowFactory>();
builder.Services.AddSingleton<IWorkflowRuntime, MafWorkflowRuntime>();

var app = builder.Build();

app.MapPost("/api/conversations/messages", async (
    ConversationRequest request,
    IConversationGateway gateway,
    CancellationToken cancellationToken) =>
{
    ConversationResponse response = await gateway.HandleAsync(request, cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/api/workflows/catalog", (IWorkflowDefinitionRegistry registry) => Results.Ok(registry.GetAll()));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
