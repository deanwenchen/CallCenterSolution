using CallCenter.AgentHost;
using CallCenter.Framework;
using CallCenter.WebApi;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

// DI registration: CallCenter framework (reads DASHSCOPE_API_KEY from environment via ApplyDefaults, Safety options from IConfiguration)
builder.Services.AddCallCenter(builder.Configuration);

// DI registration: Session store (reads SessionStore config section to switch between memory/Redis)
builder.Services.AddSessionStore(builder.Configuration);

// DI registration: AIAgentFactory (required by CallCenterService DI constructor)
builder.Services.AddSingleton<AIAgentFactory>();

// DI registration: AgentSkillsProvider (required by CallCenterService DI constructor)
builder.Services.AddSingleton<AgentSkillsProvider>(sp => new AgentSkillsProvider(SkillRegistry.All));

// DI registration: CallCenterService (external DI constructor)
builder.Services.AddScoped<CallCenterService>();

// CORS service registration — allow all origins (development mode, WA-05)
builder.Services.AddCors(options => options.AddPolicy("AllowAll",
    policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Swagger service registration (configured in Task 3)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware pipeline
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// POST /chat endpoint — blocking JSON response
app.MapPost("/chat", async (ChatRequest request, CallCenterService svc) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "message is required" });
    }

    var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
        ? Guid.NewGuid().ToString()
        : request.SessionId;

    var result = await svc.ProcessAsync(sessionId, request.Message);

    return Results.Ok(new { response = result, sessionId = sessionId });
})
.WithName("Chat");

// POST /chat/stream endpoint — SSE event stream for real-time workflow output
app.MapPost("/chat/stream", async (ChatRequest request, CallCenterService svc, HttpContext http) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "message is required" });
    }

    var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
        ? Guid.NewGuid().ToString()
        : request.SessionId;

    // Lazy session cleanup (per D-14-07): check if session expired (>60 min inactive)
    var lastActivity = await svc.GetLastActivityAsync(sessionId, http.RequestAborted);
    if (lastActivity != null && DateTime.UtcNow - lastActivity.Value >= TimeSpan.FromMinutes(60))
    {
        await svc.ClearSessionScopeAsync(sessionId, http.RequestAborted);
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        await http.Response.WriteAsync(
            $"data: {{\"type\":\"SessionExpired\",\"data\":{{\"reason\":\"60 minutes of inactivity\"}}}}\n\n",
            http.RequestAborted);
        return Results.Empty;
    }

    // Stream workflow events as SSE
    http.Response.ContentType = "text/event-stream";
    http.Response.Headers.CacheControl = "no-cache";

    await foreach (var sseEvent in svc.ProcessStreamingAsync(sessionId, request.Message, http.RequestAborted))
    {
        await http.Response.WriteAsync(sseEvent, http.RequestAborted);
        await http.Response.Body.FlushAsync(http.RequestAborted);
    }

    return Results.Empty;
})
.WithName("ChatStream");

app.Run();
