using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CallCenter.Application;
using CallCenter.Composition;
using CallCenter.Domain;
using Microsoft.Extensions.DependencyInjection;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

using ServiceProvider services = BuildServices();
using IServiceScope scope = services.CreateScope();
IConversationGateway gateway = scope.ServiceProvider.GetRequiredService<IConversationGateway>();
IWorkflowDefinitionRegistry workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRegistry>();

string sessionId = $"console-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
string userId = "console-user";
string tenantId = "default";
string channel = "console";
var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
bool exitRequested = false;
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = false,
    Converters = { new JsonStringEnumConverter() }
};

PrintWelcome();

while (!exitRequested)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write($"[{sessionId}]> ");
    Console.ResetColor();

    string? input = Console.ReadLine();
    if (input is null)
    {
        break;
    }

    input = input.Trim('\uFEFF').Trim();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (HandleCommand(input))
    {
        continue;
    }

    var request = new ConversationRequest(
        sessionId,
        userId,
        channel,
        tenantId,
        input,
        AuthToken: "console-token",
        Metadata: metadata.Count == 0 ? null : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));

    try
    {
        ConversationResponse response = await gateway.HandleAsync(request).ConfigureAwait(false);
        PrintResponse(response);
    }
    catch (Exception ex)
    {
        PrintError(ex);
    }
}

bool HandleCommand(string input)
{
    if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        exitRequested = true;
        return true;
    }

    if (input.Equals("/help", StringComparison.OrdinalIgnoreCase))
    {
        PrintHelp();
        return true;
    }

    if (input.Equals("/catalog", StringComparison.OrdinalIgnoreCase))
    {
        PrintCatalog();
        return true;
    }

    if (input.Equals("/new", StringComparison.OrdinalIgnoreCase))
    {
        sessionId = $"console-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        metadata.Clear();
        Console.WriteLine($"New session: {sessionId}");
        return true;
    }

    if (input.StartsWith("/session ", StringComparison.OrdinalIgnoreCase))
    {
        sessionId = input["/session ".Length..].Trim();
        Console.WriteLine($"Session switched to: {sessionId}");
        return true;
    }

    if (input.StartsWith("/user ", StringComparison.OrdinalIgnoreCase))
    {
        userId = input["/user ".Length..].Trim();
        Console.WriteLine($"User switched to: {userId}");
        return true;
    }

    if (input.StartsWith("/tenant ", StringComparison.OrdinalIgnoreCase))
    {
        tenantId = input["/tenant ".Length..].Trim();
        Console.WriteLine($"Tenant switched to: {tenantId}");
        return true;
    }

    if (input.StartsWith("/channel ", StringComparison.OrdinalIgnoreCase))
    {
        channel = input["/channel ".Length..].Trim();
        Console.WriteLine($"Channel switched to: {channel}");
        return true;
    }

    if (input.StartsWith("/meta ", StringComparison.OrdinalIgnoreCase))
    {
        SetMetadata(input["/meta ".Length..].Trim());
        return true;
    }

    if (input.Equals("/clear-meta", StringComparison.OrdinalIgnoreCase))
    {
        metadata.Clear();
        Console.WriteLine("Metadata cleared.");
        return true;
    }

    return false;
}

void SetMetadata(string expression)
{
    int separator = expression.IndexOf('=', StringComparison.Ordinal);
    if (separator <= 0)
    {
        Console.WriteLine("Usage: /meta key=value");
        return;
    }

    string key = expression[..separator].Trim();
    string value = expression[(separator + 1)..].Trim();
    metadata[key] = value;
    Console.WriteLine($"Metadata set: {key}={value}");
}

void PrintWelcome()
{
    Console.WriteLine("CallCenter Console Client");
    Console.WriteLine("Type /help for commands. Type /exit to quit.");
    Console.WriteLine();
    PrintExamples();
}

void PrintHelp()
{
    Console.WriteLine("Commands:");
    Console.WriteLine("  /help                 Show help.");
    Console.WriteLine("  /catalog              Show registered workflows.");
    Console.WriteLine("  /new                  Start a new session.");
    Console.WriteLine("  /session <id>         Switch session id.");
    Console.WriteLine("  /user <id>            Switch user id.");
    Console.WriteLine("  /tenant <id>          Switch tenant id.");
    Console.WriteLine("  /channel <name>       Switch channel.");
    Console.WriteLine("  /meta key=value       Set request metadata.");
    Console.WriteLine("  /clear-meta           Clear request metadata.");
    Console.WriteLine("  /exit                 Quit.");
    Console.WriteLine();
    PrintExamples();
}

void PrintExamples()
{
    Console.WriteLine("Examples:");
    Console.WriteLine("  refund order ORD-10001 amount 99");
    Console.WriteLine("  product return order ORD-10001 amount 99");
    Console.WriteLine("  yes");
    Console.WriteLine("  create invoice for order ORD-20001 invoice title ACME amount 100");
    Console.WriteLine("  show my member points");
    Console.WriteLine("  track logistics for order ORD-30001");
    Console.WriteLine("  issue coupon for me");
    Console.WriteLine("  /meta blacklisted=true");
    Console.WriteLine();
}

void PrintCatalog()
{
    foreach (WorkflowDefinition workflow in workflowRegistry.GetAll().OrderBy(workflow => workflow.Name))
    {
        Console.WriteLine($"{workflow.Name} [{workflow.Capability}]");
        foreach (WorkflowStepDefinition step in workflow.Steps)
        {
            string human = step.HumanInTheLoop ? " human-in-loop" : string.Empty;
            Console.WriteLine($"  - {step.Name} -> {step.BusinessActionName}{human}");
        }
    }

    Console.WriteLine();
}

void PrintResponse(ConversationResponse response)
{
    Console.ForegroundColor = response.Status == WorkflowStatus.Failed ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine(response.Message);
    Console.ResetColor();

    Console.WriteLine($"  status: {response.Status}");
    Console.WriteLine($"  workflow: {response.WorkflowName ?? "-"}");
    Console.WriteLine($"  step: {response.CurrentStep ?? "-"}");
    Console.WriteLine($"  instance: {response.WorkflowInstanceId}");

    if (response.Data.Count > 0)
    {
        string json = JsonSerializer.Serialize(response.Data, jsonOptions);
        Console.WriteLine($"  data: {json}");
    }

    Console.WriteLine();
}

void PrintError(Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(ex.Message);
    Console.ResetColor();
    Console.WriteLine();
}

static ServiceProvider BuildServices()
{
    var services = new ServiceCollection();
    services.AddCallCenter();
    return services.BuildServiceProvider(validateScopes: true);
}
