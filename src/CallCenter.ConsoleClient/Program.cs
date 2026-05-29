using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CallCenter.Core;
using CallCenter.Composition;
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

// 调试用模拟问法流程放在这里。
// 需要断点调试完整链路时，把 RunDebugConversationDemo 改成 true。
bool RunDebugConversationDemo = false;
if (RunDebugConversationDemo)
{
    await RunDebugConversationDemoAsync(gateway).ConfigureAwait(false);
    return;
}

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
        Console.WriteLine($"{workflow.Name} [{workflow.DisplayCapability}]");
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

async Task RunDebugConversationDemoAsync(IConversationGateway demoGateway)
{
    var scenarios = new (string Name, string UserId, string[] Messages, Dictionary<string, string>? Metadata)[]
    {
        (
            "退款流程：先申请退款，再确认继续",
            "debug-refund-user",
            [
                "refund order ORD-10001 amount 99",
                "yes"
            ],
            null
        ),
        (
            "退货流程：先申请退货，再确认继续",
            "debug-return-user",
            [
                "product return order ORD-10002 amount 129",
                "yes"
            ],
            null
        ),
        (
            "发票流程：直接开票",
            "debug-invoice-user",
            [
                "create invoice for order ORD-20001 invoice title ACME amount 100"
            ],
            null
        ),
        (
            "物流流程：查询订单物流",
            "debug-logistics-user",
            [
                "track logistics for order ORD-30001"
            ],
            null
        ),
        (
            "会员流程：查询会员积分",
            "debug-member-user",
            [
                "show my member points"
            ],
            null
        ),
        (
            "优惠券流程：发券",
            "debug-coupon-user",
            [
                "issue coupon for me"
            ],
            null
        ),
        (
            "人工客服流程：明确转人工",
            "debug-human-user",
            [
                "human agent please"
            ],
            null
        ),
        (
            "黑名单拦截：通过 metadata 模拟风控拒绝",
            "debug-blocked-user",
            [
                "refund order ORD-90001 amount 10"
            ],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["blacklisted"] = "true"
            }
        )
    };

    foreach ((string name, string scenarioUserId, string[] messages, Dictionary<string, string>? scenarioMetadata) in scenarios)
    {
        string demoSessionId = $"debug-{Guid.NewGuid():N}";
        Console.WriteLine($"=== {name} ===");
        Console.WriteLine($"session: {demoSessionId}");

        foreach (string message in messages)
        {
            var request = new ConversationRequest(
                demoSessionId,
                scenarioUserId,
                "console-debug",
                "default",
                message,
                AuthToken: "console-token",
                Metadata: scenarioMetadata is null
                    ? null
                    : new Dictionary<string, string>(scenarioMetadata, StringComparer.OrdinalIgnoreCase));

            Console.WriteLine($"> {message}");
            ConversationResponse response = await demoGateway.HandleAsync(request).ConfigureAwait(false);
            PrintResponse(response);
        }
    }
}

static ServiceProvider BuildServices()
{
    var services = new ServiceCollection();
    services.AddCallCenter();
    return services.BuildServiceProvider(validateScopes: true);
}
