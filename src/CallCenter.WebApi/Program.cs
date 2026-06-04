using CallCenter.AgentHost;
using CallCenter.Framework;
using CallCenter.WebApi;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

// DI registration: CallCenter framework (reads DASHSCOPE_API_KEY from environment via ApplyDefaults)
builder.Services.AddCallCenter();

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

app.Run();
