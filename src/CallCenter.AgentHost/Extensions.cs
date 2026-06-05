using CallCenter.Framework;
using CallCenter.Framework.Audit;
using CallCenter.Framework.EventBus;
using CallCenter.Framework.Pipeline;
using CallCenter.Framework.Safety;
using CallCenter.Framework.Session;
using CallCenter.Shared.Mcp;
using CallCenter.Shared.Services;
using CallCenter.Workflows.Refund;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
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
    /// 注册 CallCenter 框架所有基础设施，并从配置读取安全选项。
    /// 适用于 WebApi 场景，从 appsettings.json 读取 "Safety" 配置段。
    /// </summary>
    public static IServiceCollection AddCallCenter(
        this IServiceCollection services,
        IConfiguration configuration,
        CallCenterOptions? options = null)
    {
        // 手动绑定安全选项（避免依赖 Configuration.Binder）
        var safetySection = configuration.GetSection("Safety");
        var safetyOptions = new SafetyOptions
        {
            EnableKeywordFilter = bool.TryParse(safetySection["EnableKeywordFilter"], out var ekf) ? ekf : true,
            EnableInjectionDetection = bool.TryParse(safetySection["EnableInjectionDetection"], out var eid) ? eid : true,
            BlockedKeywords = safetySection.GetSection("BlockedKeywords").GetChildren().Select(c => c.Value).Where(v => v != null).ToArray()!,
            BlockedMessageTemplate = safetySection["BlockedMessageTemplate"] ?? "您的输入包含敏感内容（{keyword}），我们已暂时中止处理。如有需要，请联系人工客服。",
            // Output-end content filtering
            BlockedOutputCategories = safetySection.GetSection("BlockedOutputCategories").GetChildren().Select(c => c.Value).Where(v => v != null).ToArray()!,
            ViolenceKeywords = safetySection.GetSection("ViolenceKeywords").GetChildren().Select(c => c.Value).Where(v => v != null).ToArray()!,
            PornographyKeywords = safetySection.GetSection("PornographyKeywords").GetChildren().Select(c => c.Value).Where(v => v != null).ToArray()!,
            PoliticsKeywords = safetySection.GetSection("PoliticsKeywords").GetChildren().Select(c => c.Value).Where(v => v != null).ToArray()!,
            ViolenceMessageTemplate = safetySection["ViolenceMessageTemplate"] ?? "抱歉，系统无法提供相关内容。如需帮助，请联系人工客服。",
            PornographyMessageTemplate = safetySection["PornographyMessageTemplate"] ?? "抱歉，系统无法提供相关内容。如需帮助，请联系人工客服。",
            PoliticsMessageTemplate = safetySection["PoliticsMessageTemplate"] ?? "抱歉，该问题超出我的服务范围。如有其他问题，我很乐意帮助您。",
        };
        // 如果配置中没有 BlockedKeywords，回退到默认值
        if (safetyOptions.BlockedKeywords.Length == 0)
        {
            safetyOptions.BlockedKeywords = new SafetyOptions().BlockedKeywords;
        }

        services.AddSingleton(safetyOptions);
        services.AddSingleton<KeywordFilter>(sp => new KeywordFilter(sp.GetRequiredService<SafetyOptions>()));

        return AddCallCenterCore(services, options, useConfiguredKeywordFilter: true);
    }

    /// <summary>
    /// 注册 CallCenter 框架所有基础设施。
    /// 包括：LLM 客户端（keyed + pipeline）、会话存储、审计日志、事件总线、
    /// Mock 服务、工作流实例。
    /// </summary>
    public static IServiceCollection AddCallCenter(
        this IServiceCollection services,
        CallCenterOptions? options = null)
    {
        return AddCallCenterCore(services, options, useConfiguredKeywordFilter: false);
    }

    private static IServiceCollection AddCallCenterCore(
        IServiceCollection services,
        CallCenterOptions? options,
        bool useConfiguredKeywordFilter)
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
        // 使用工厂委托延迟解析 KeywordFilter 和 SafetyOptions，确保 DI 已就绪
        services.AddSingleton<IChatClient>(sp =>
        {
            var keywordFilter = useConfiguredKeywordFilter ? sp.GetService<KeywordFilter>() : null;
            var safetyOptions = sp.GetService<SafetyOptions>();
            return StandardPipelineFactory.CreatePipeline(baseClient, summarizerClient, "pipeline", logger: null, keywordFilter: keywordFilter, safetyOptions: safetyOptions);
        });

        // 会话存储（同时注册具体实现和接口，向后兼容）
        services.AddSingleton<InMemorySessionStore>();
        services.AddSingleton<ISessionStore>(sp => sp.GetRequiredService<InMemorySessionStore>());

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

    /// <summary>
    /// 根据配置注册 ISessionStore 实现（内存或 Redis）。
    /// 读取 appsettings.json 中 "SessionStore" 配置段：
    ///   - Provider: "memory"（默认）或 "redis"
    ///   - ProviderName: Redis provider 名称（默认 "default"）
    ///   - DbIndex: Redis 数据库索引（默认 0）
    ///   - DefaultTtlMinutes: 默认 TTL 分钟数（默认 30）
    /// </summary>
    public static IServiceCollection AddSessionStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("SessionStore");
        var provider = section["Provider"] ?? "memory";

        if (string.Equals(provider, "redis", StringComparison.OrdinalIgnoreCase))
        {
            var redisProviderName = section["ProviderName"] ?? "default";
            var dbIndex = int.TryParse(section["DbIndex"], out var db) ? db : 0;
            var ttlMinutes = int.TryParse(section["DefaultTtlMinutes"], out var ttl) ? ttl : 30;
            var defaultTtl = TimeSpan.FromMinutes(ttlMinutes);

            services.AddSingleton<ISessionStore>(sp =>
                new RedisSessionStore(redisProviderName, dbIndex, defaultTtl));
        }
        else
        {
            services.AddSingleton<ISessionStore, InMemorySessionStore>();
        }

        return services;
    }
}
