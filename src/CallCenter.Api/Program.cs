using CallCenter.Application;
using CallCenter.Composition;
using CallCenter.Domain;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCallCenter();

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

app.MapPost("/api/knowledge/search", async (
    ConversationRequest request,
    IConversationContextFactory contextFactory,
    IIntentRecognizer intentRecognizer,
    IKnowledgeService knowledgeService,
    CancellationToken cancellationToken) =>
{
    SessionContext session = contextFactory.Create(request);
    IntentResult intent = await intentRecognizer.RecognizeAsync(session, request.Message, cancellationToken);
    KnowledgeSearchResult result = await knowledgeService.SearchAsync(session, request.Message, intent.Entities, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
