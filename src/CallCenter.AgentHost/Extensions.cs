using CallCenter.Framework;
using CallCenter.Framework.Audit;
using CallCenter.Framework.EventBus;
using CallCenter.Framework.Pipeline;
using CallCenter.Framework.Session;
using CallCenter.Shared.Mcp;
using CallCenter.Shared.Services;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using System;
using System.ClientModel;

namespace CallCenter.AgentHost;

/// <summary>
/// CallCenter 框架 DI 扩展方法。
/// 主要作用：提供 AddCallCenter() 一站式注册所有基础设施，
/// 替代 Program.cs 中 80+ 行的手动装配代码。
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 注册 CallCenter 框架所有基础设施。
    /// 包括：LLM 客户端（keyed + pipeline）、会话存储、审计日志、事件总线、
    /// Mock 服务、工作流实例。
    /// </summary>
    public static IServiceCollection AddCallCenter(
        this IServiceCollection services,
        CallCenterOptions? options = null)
    {
        options ??= new CallCenterOptions();
        options.ApplyDefaults();

        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new InvalidOperationException("DASHSCOPE_API_KEY not set. Set it to your DashScope API key.");
        }

        // 创建 OpenAI 客户端（DashScope 兼容端点）
        var openAIClient = new OpenAIClient(
            new ApiKeyCredential(options.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) });

        // 原始 LLM 客户端（keyed 注册，供需要直接访问的场景使用）
        var baseClient = openAIClient.GetChatClient(options.ModelName).AsIChatClient();
        services.AddKeyedSingleton<IChatClient>("base", baseClient);

        // 摘要压缩客户端（用于上下文压缩层）
        var summarizerClient = StandardPipelineFactory.CreateSummarizerClient(openAIClient, options.ModelName);

        // 6 层管道客户端（安全 → 日志 → 压缩 → 工具审批 → LLM → 安全输出）
        var pipelineClient = StandardPipelineFactory.CreatePipeline(baseClient, summarizerClient, "pipeline", logger: null);
        services.AddSingleton<IChatClient>(pipelineClient);

        // 会话存储
        services.AddSingleton<InMemorySessionStore>();

        // 审计日志
        services.AddSingleton(new AuditLogger(".audit"));

        // 事件总线
        services.AddSingleton<IBusinessEventBus, InMemoryBusinessEventBus>();

        // JSONL 日志记录器
        services.AddSingleton<CallCenter.Framework.Logging.JsonlLogger>();

        // Mock 服务（默认注册，可被后续 override 方法替换）
        if (options.UseMockServices)
        {
            services.AddSingleton<IOrderMcpClient, MockOrderService>();
            services.AddSingleton<IFinanceMcpClient, MockFinanceService>();
            services.AddSingleton<IMemberMcpClient, MockMemberService>();
        }

        // 退款工作流
        services.AddSingleton<Workflow>(sp => RefundWorkflow.Build(
            sp.GetRequiredService<IOrderMcpClient>(),
            sp.GetRequiredService<IFinanceMcpClient>(),
            sp.GetRequiredService<IMemberMcpClient>(),
            sp.GetRequiredService<IBusinessEventBus>()));

        // 退款完成事件回调
        if (options.OnRefundCompleted != null)
        {
            var capturedCallback = options.OnRefundCompleted;
            services.AddSingleton<IBusinessEventBus>(sp =>
            {
                var eventBus = new InMemoryBusinessEventBus();
                eventBus.Subscribe<RefundCompletedEvent>(e =>
                {
                    capturedCallback(e);
                    return Task.CompletedTask;
                });
                return eventBus;
            });
        }

        return services;
    }

    /// <summary>用自定义实现替换 IOrderMcpClient 注册。</summary>
    public static IServiceCollection AddCallCenterOrderService<T>(this IServiceCollection services)
        where T : class, IOrderMcpClient
    {
        services.AddSingleton<IOrderMcpClient, T>();
        return services;
    }

    /// <summary>用自定义实现替换 IFinanceMcpClient 注册。</summary>
    public static IServiceCollection AddCallCenterFinanceService<T>(this IServiceCollection services)
        where T : class, IFinanceMcpClient
    {
        services.AddSingleton<IFinanceMcpClient, T>();
        return services;
    }

    /// <summary>用自定义实现替换 IMemberMcpClient 注册。</summary>
    public static IServiceCollection AddCallCenterMemberService<T>(this IServiceCollection services)
        where T : class, IMemberMcpClient
    {
        services.AddSingleton<IMemberMcpClient, T>();
        return services;
    }
}
