#pragma warning disable MAAI001
using System.Threading.Channels;
using CallCenter.Framework;
using CallCenter.Framework.Audit;
using CallCenter.Framework.EventBus;
using CallCenter.Framework.Logging;
using CallCenter.Framework.Parsing;
using CallCenter.Framework.Session;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.AgentHost;

/// <summary>
/// CallCenter 服务核心。
/// 主要作用：定义整个 CallCenterService 的基础骨架、依赖字段和两种实例化方式。
/// 后续 Intent/Routing/Execution/Interaction 所有 partial 文件都共享此文件的字段声明。
/// </summary>
public partial class CallCenterService : IDisposable
{
    // ===== Fields =====

    private readonly IServiceProvider? _provider;
    private readonly CallCenterOptions _options;
    private readonly IChatClient _chatClient;
    private readonly AIAgentFactory _agentFactory;
    private readonly InMemorySessionStore _sessionStore;
    private readonly CheckpointManager _checkpointManager;
    private readonly AuditLogger _auditLogger;
    private readonly IBusinessEventBus _eventBus;
    private readonly JsonlLogger _logger;
    private readonly Workflow _refundWorkflow;
    private readonly AgentSkillsProvider _skillsProvider;
    private readonly Channel<string> _inputChannel;
    private readonly CancellationTokenSource _inputCts = new();
    private readonly Func<string, CancellationToken, Task<IntentResult?>> _recognizeIntent;
    private bool _disposed;

    // ===== Parameterless Constructor (self-builds DI container) =====
    // For console/standalone scenarios — builds its own IServiceProvider.

    public CallCenterService(CallCenterOptions? options = null)
    {
        _options = options ?? new CallCenterOptions();
        _options.ApplyDefaults();

        var services = new ServiceCollection();

        // Register all CallCenter infrastructure (LLM, SessionStore, AuditLogger, EventBus, Mock services, Workflow)
        services.AddCallCenter(_options);

        // Register Agent factory
        services.AddSingleton<AIAgentFactory>();

        // Register AgentSkillsProvider (not registered by AddCallCenter)
        services.AddSingleton<AgentSkillsProvider>(sp =>
            new AgentSkillsProvider(SkillRegistry.All));

        _provider = services.BuildServiceProvider();

        // Resolve from the self-built provider
        _chatClient = _provider.GetRequiredService<IChatClient>();
        _agentFactory = _provider.GetRequiredService<AIAgentFactory>();
        _sessionStore = _provider.GetRequiredService<InMemorySessionStore>();
        _auditLogger = _provider.GetRequiredService<AuditLogger>();
        _eventBus = _provider.GetRequiredService<IBusinessEventBus>();
        _eventBus.Subscribe<RefundCompletedEvent>(async e =>
        {
            Console.WriteLine($"\n[EVENT] 退款完成: 订单{e.OrderId}, 金额 {e.RefundAmount:C}");
            await Task.CompletedTask;
        });
        _logger = _provider.GetRequiredService<JsonlLogger>();
        _refundWorkflow = _provider.GetRequiredService<Workflow>();
        _skillsProvider = _provider.GetRequiredService<AgentSkillsProvider>();

        _checkpointManager = CheckpointManager.Default;

        _inputChannel = Channel.CreateUnbounded<string>();

        // Start stdin reader background task (decouple console input from event loop)
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_inputCts.Token.IsCancellationRequested)
                {
                    var line = await Console.In.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;
                    await _inputChannel.Writer.WriteAsync(line, _inputCts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        });

        // Build the intent recognition delegate
        _recognizeIntent = BuildRecognizeIntentDelegate();
    }

    // ===== DI Injection Constructor (external DI container) =====
    // For Web API / hosted scenarios — accepts external IServiceProvider.

    public CallCenterService(IServiceProvider provider)
    {
        _provider = null; // External DI container manages lifecycle

        _options = new CallCenterOptions();

        // Resolve all dependencies from external provider
        _chatClient = provider.GetRequiredService<IChatClient>();
        _agentFactory = provider.GetRequiredService<AIAgentFactory>();
        _sessionStore = provider.GetRequiredService<InMemorySessionStore>();
        _auditLogger = provider.GetRequiredService<AuditLogger>();
        _eventBus = provider.GetRequiredService<IBusinessEventBus>();
        _eventBus.Subscribe<RefundCompletedEvent>(async e =>
        {
            Console.WriteLine($"\n[EVENT] 退款完成: 订单{e.OrderId}, 金额 {e.RefundAmount:C}");
            await Task.CompletedTask;
        });
        _logger = provider.GetRequiredService<JsonlLogger>();
        _refundWorkflow = provider.GetRequiredService<Workflow>();
        _skillsProvider = provider.GetRequiredService<AgentSkillsProvider>();

        _checkpointManager = CheckpointManager.Default;

        _inputChannel = Channel.CreateUnbounded<string>();

        // Build the intent recognition delegate
        _recognizeIntent = BuildRecognizeIntentDelegate();
    }

    // ===== Intent Recognition Helper =====

    private Func<string, CancellationToken, Task<IntentResult?>> BuildRecognizeIntentDelegate()
    {
        return async (userMessage, ct) =>
        {
            try
            {
                var intentAgent = _agentFactory.CreateIntentAgent(_skillsProvider);
                var response = await intentAgent.RunAsync(userMessage, cancellationToken: ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(response?.Text))
                    return null;

                var parser = new StructuredOutputParser<IntentResult>();
                return parser.Parse(response.Text);
            }
            catch
            {
                return null;
            }
        };
    }

    // ===== IDisposable =====

    public void Dispose()
    {
        if (_disposed) return;

        _inputCts.Cancel();
        _inputChannel.Writer.TryComplete();

        // Only dispose the self-built provider (not the external one)
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}
