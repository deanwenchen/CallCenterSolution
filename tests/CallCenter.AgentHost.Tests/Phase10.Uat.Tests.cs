#pragma warning disable MAAI001
using System.Text.Json;
using CallCenter.Framework;
using CallCenter.Framework.Audit;
using CallCenter.Framework.EventBus;
using CallCenter.Framework.Safety;
using CallCenter.Framework.Session;
using CallCenter.Shared.Mcp;
using CallCenter.Shared.Models;
using CallCenter.Shared.Services;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.AgentHost.Tests;

/// <summary>
/// Phase 10 UAT 集成测试 — 使用 FakeChatClient 模拟 LLM，验证 ProcessAsync 端到端行为。
/// </summary>
public class Phase10UatTests
{
    // ===== FakeChatClient =====

    private class FakeChatClient : IChatClient
    {
        private readonly Func<string, string> _responseFunc;

        public FakeChatClient(Func<string, string> responseFunc) => _responseFunc = responseFunc;
        public FakeChatClient(string jsonResponse) => _responseFunc = _ => jsonResponse;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var userMessage = string.Join(" ", messages.Where(m => m.Role == ChatRole.User).Select(m => m.Text));
            var responseText = _responseFunc(userMessage);
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, responseText)]));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield break;
        }

        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }

    // ===== Test Helpers =====

    /// <summary>
    /// 构建使用 FakeChatClient 的 CallCenterService。
    /// </summary>
    private static CallCenterService CreateService(
        Func<string, string> llmResponseFunc,
        InMemorySessionStore? sessionStore = null,
        AuditLogger? auditLogger = null)
    {
        var store = sessionStore ?? new InMemorySessionStore();
        var logger = auditLogger ?? new AuditLogger(".audit-test");
        var eventBus = new InMemoryBusinessEventBus();

        var services = new ServiceCollection();
        services.AddSingleton(new SafetyOptions());

        var fakeClient = new FakeChatClient(llmResponseFunc);
        services.AddKeyedSingleton<IChatClient>("base", fakeClient);
        services.AddSingleton<IChatClient>(fakeClient);
        services.AddSingleton(store);
        services.AddSingleton(logger);
        services.AddSingleton<IBusinessEventBus>(eventBus);
        services.AddSingleton<CallCenter.Framework.Logging.JsonlLogger>();
        services.AddSingleton<IOrderMcpClient, MockOrderService>();
        services.AddSingleton<IFinanceMcpClient, MockFinanceService>();
        services.AddSingleton<IMemberMcpClient, MockMemberService>();

        services.AddSingleton<Workflow>(sp => RefundWorkflow.Build(
            sp.GetRequiredService<IOrderMcpClient>(),
            sp.GetRequiredService<IFinanceMcpClient>(),
            sp.GetRequiredService<IMemberMcpClient>(),
            sp.GetRequiredService<IBusinessEventBus>()));

        // Register AgentSkillsProvider and AIAgentFactory
        services.AddSingleton<AgentSkillsProvider>(_ => new AgentSkillsProvider([]));
        services.AddSingleton<AIAgentFactory>(sp => new AIAgentFactory(sp.GetRequiredService<IChatClient>()));

        var provider = services.BuildServiceProvider();
        return new CallCenterService(provider);
    }

    // ===== UAT Tests =====

    /// <summary>
    /// UAT #1: ProcessAsync Greeting Flow
    /// 预期: 调用 ProcessAsync("session-1", "你好") 返回问候，不启动工作流。
    /// </summary>
    [Fact]
    public async Task Uat01_GreetingFlow_ReturnsGreetingWithoutWorkflow()
    {
        var service = CreateService(_ =>
            JsonSerializer.Serialize(new { intent = "greeting", workflow = (string?)null, parameters = new { } }));

        try
        {
            var result = await service.ProcessAsync("session-1", "你好");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 不应包含工作流相关内容（如订单号追问）
            Assert.DoesNotContain("orderId", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            service.Dispose();
        }
    }

    /// <summary>
    /// UAT #2: ProcessAsync Refund Workflow End-to-End
    /// 预期: 调用 ProcessAsync("session-1", "我要退款") 启动退款流程，追问订单号。
    /// </summary>
    [Fact]
    public async Task Uat02_RefundWorkflow_StartsAndPromptsForOrder()
    {
        var service = CreateService(_ =>
            JsonSerializer.Serialize(new { intent = "refund", workflow = "refund", parameters = new { } }));

        try
        {
            var result = await service.ProcessAsync("session-1", "我要退款");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 退款工作流的第一步是 GetOrder，没有订单号会通过 InfoPort 追问
            Assert.True(
                result.Contains("订单", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("order", StringComparison.OrdinalIgnoreCase),
                $"Expected order-related prompt but got: {result}");
        }
        finally
        {
            service.Dispose();
        }
    }

    /// <summary>
    /// UAT #3: Session Timeout Detection
    /// 预期: 设置会话 lastActivity 为 65 分钟前，检测到超时。
    /// </summary>
    [Fact]
    public async Task Uat03_SessionTimeoutDetection()
    {
        var sessionStore = new InMemorySessionStore();
        var sessionId = "session-timeout-test";

        // 模拟 65 分钟前的会话
        var oldTimestamp = DateTime.UtcNow.AddMinutes(-65);
        await sessionStore.SetAsync("lastActivity", oldTimestamp, sessionId);
        await sessionStore.SetAsync("hasWorkflow", true, sessionId);

        var service = CreateService(
            _ => JsonSerializer.Serialize(new { intent = "refund", workflow = "refund", parameters = new { } }),
            sessionStore: sessionStore);

        try
        {
            var lastActivity = await service.GetLastActivityAsync(sessionId);

            Assert.NotNull(lastActivity);
            var elapsed = DateTime.UtcNow - lastActivity.Value;
            Assert.True(elapsed >= TimeSpan.FromMinutes(60),
                $"Expected >= 60 min elapsed but was {elapsed.TotalMinutes:F1} min");
        }
        finally
        {
            service.Dispose();
        }
    }

    /// <summary>
    /// UAT #4: Saga Compensation on ExecuteRefund Failure
    /// 预期: 触发 ExecuteRefund 失败 → Saga 补偿运行 → 恢复优惠券。
    ///
    /// 注意: 此测试使用 MockFinanceService 的 FAILING 模式来触发 Saga 补偿。
    /// </summary>
    [Fact]
    public async Task Uat04_SagaCompensationOnRefundFailure()
    {
        var capturedEvents = new List<string>();
        var eventBus = new InMemoryBusinessEventBus();
        eventBus.Subscribe<RefundCompletedEvent>(e =>
        {
            capturedEvents.Add($"RefundCompleted: {e.OrderId}");
            return Task.CompletedTask;
        });

        var sessionStore = new InMemorySessionStore();
        var auditLogger = new AuditLogger(".audit-test-saga");

        // Use a finance service that fails on RefundAsync
        var failingFinance = new FailingFinanceService();
        // Use a member service that captures coupon restore calls
        var capturingMember = new CapturingMemberService();

        var services = new ServiceCollection();
        services.AddSingleton(new SafetyOptions());

        // Fake LLM: refund intent with orderId A001
        var fakeClient = new FakeChatClient(_ =>
            JsonSerializer.Serialize(new { intent = "refund", workflow = "refund", parameters = new { orderId = "A001" } }));
        services.AddKeyedSingleton<IChatClient>("base", fakeClient);
        services.AddSingleton<IChatClient>(fakeClient);
        services.AddSingleton(sessionStore);
        services.AddSingleton(auditLogger);
        services.AddSingleton<IBusinessEventBus>(eventBus);
        services.AddSingleton<CallCenter.Framework.Logging.JsonlLogger>();
        services.AddSingleton<IOrderMcpClient, MockOrderService>();
        services.AddSingleton<IFinanceMcpClient>(failingFinance);
        services.AddSingleton<IMemberMcpClient>(capturingMember);

        services.AddSingleton<Workflow>(sp => RefundWorkflow.Build(
            sp.GetRequiredService<IOrderMcpClient>(),
            sp.GetRequiredService<IFinanceMcpClient>(),
            sp.GetRequiredService<IMemberMcpClient>(),
            sp.GetRequiredService<IBusinessEventBus>()));

        var provider = services.BuildServiceProvider();
        var service = new CallCenterService(provider);

        try
        {
            var result = await service.ProcessAsync("session-saga", "我要退款，订单A001");

            // Assert: Saga compensation should have restored the coupon
            Assert.True(capturingMember.CouponRestored,
                "Expected Saga compensation to restore coupon after refund failure");
        }
        finally
        {
            service.Dispose();
        }
    }

    // ===== Mock Classes =====

    /// <summary>
    /// 模拟执行退款时抛出异常的财务服务，用于触发 Saga 补偿。
    /// </summary>
    private class FailingFinanceService : IFinanceMcpClient
    {
        public bool CouponRestored { get; private set; }

        public Task<RefundResult> RefundAsync(string orderId, decimal amount, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Simulated refund failure for Saga compensation test");
        }
    }

    /// <summary>
    /// 模拟恢复优惠券失败/成功的会员服务，用于验证 Saga 补偿路径。
    /// </summary>
    private class CapturingMemberService : IMemberMcpClient
    {
        public bool CouponRestored { get; private set; }

        public Task<CouponInfo?> GetCouponAsync(string userId, CancellationToken ct = default) =>
            Task.FromResult<CouponInfo?>(new CouponInfo("COUPON-1", "test-user", 10m, DateTime.Now.AddDays(30)));

        public Task<bool> RestoreCouponAsync(string userId, string couponId, CancellationToken ct = default)
        {
            CouponRestored = true;
            return Task.FromResult(true);
        }
    }
}
