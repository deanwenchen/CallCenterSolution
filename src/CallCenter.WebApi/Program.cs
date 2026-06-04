using CallCenter.AgentHost;
using CallCenter.Framework;
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

// CORS service registration (strategy "AllowAll", configured in Task 3)
builder.Services.AddCors();

// Swagger service registration (configured in Task 3)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware pipeline (configured in Task 3)

app.Run();
