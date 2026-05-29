using System.Reflection;
using CallCenter.Core;
using CallCenter.BusinessActions;
using CallCenter.Infrastructure;
using CallCenter.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Composition;

/// <summary>
/// 客服系统统一注册入口。
/// </summary>
public static class CallCenterServiceCollectionExtensions
{
    /// <summary>
    /// 注册客服系统核心服务，并自动发现模块里的流程、能力和业务动作。
    /// </summary>
    public static IServiceCollection AddCallCenter(this IServiceCollection services, params Assembly[] moduleAssemblies)
    {
        return AddCallCenter(services, configuration: null, moduleAssemblies);
    }

    /// <summary>
    /// 注册客服系统核心服务，并从配置读取可选的大模型客户端设置。
    /// </summary>
    public static IServiceCollection AddCallCenter(this IServiceCollection services, IConfiguration? configuration, params Assembly[] moduleAssemblies)
    {
        Assembly[] assemblies = ResolveAssemblies(moduleAssemblies);

        services.AddSingleton(CreateQwenModelClientOptions(configuration));

        services.AddScoped<IConversationGateway, ConversationGateway>();

        services.AddSingleton<HttpClient>();
        services.AddSingleton<IAuthenticationService, MetadataAuthenticationService>();
        services.AddSingleton<IAuthorizationService, MetadataAuthorizationService>();
        services.AddSingleton<IRateLimiter, FixedWindowRateLimiter>();
        services.AddSingleton<IBlacklistService, MetadataBlacklistService>();
        services.AddSingleton<IAuditSink, FileAuditSink>();
        services.AddSingleton<IObservabilitySink, FileObservabilitySink>();
        services.AddSingleton<ISessionStore, FileSessionStore>();
        services.AddSingleton<IConversationContextFactory, ConversationContextFactory>();
        services.AddSingleton<KeywordIntentRecognizer>();
        services.AddSingleton<IModelClient, AgentFrameworkQwenModelClient>();
        services.AddSingleton<IIntentRecognizer, HybridIntentRecognizer>();
        services.AddSingleton<IPlanner, IntentCapabilityPlanner>();
        services.AddSingleton<IExternalSystemGateway, InMemoryExternalSystemGateway>();
        services.AddSingleton<IKnowledgeService, InMemoryKnowledgeService>();
        services.AddSingleton<IHumanAgentService, InMemoryHumanAgentService>();

        RegisterImplementations<ICapability>(services, assemblies);
        RegisterImplementations<IBusinessAction>(services, assemblies);
        RegisterImplementations<IWorkflowDefinitionProvider>(services, assemblies);
        RegisterImplementations<IIntentDefinitionProvider>(services, assemblies);
        RegisterImplementations<IIntentCapabilityRouteProvider>(services, assemblies);
        RegisterImplementations<ICapabilityWorkflowRouteProvider>(services, assemblies);
        RegisterImplementations<IWorkflowPermissionProvider>(services, assemblies);

        services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();
        services.AddSingleton<IBusinessActionRegistry, BusinessActionRegistry>();
        services.AddSingleton<IWorkflowDefinitionRegistry, WorkflowDefinitionRegistry>();
        services.AddSingleton<AgentFrameworkWorkflowFactory>();
        services.AddSingleton<IWorkflowRuntime, AgentFrameworkWorkflowRuntime>();

        return services;
    }

    private static QwenModelClientOptions CreateQwenModelClientOptions(IConfiguration? configuration)
    {
        var options = new QwenModelClientOptions();
        IConfigurationSection? section = configuration?.GetSection("ModelClient");
        if (section is not null)
        {
            options.Endpoint = ReadSetting(section["Endpoint"], options.Endpoint) ?? options.Endpoint;
            options.ApiKey = ReadSetting(section["ApiKey"], options.ApiKey);
            options.Model = ReadSetting(section["Model"], options.Model) ?? options.Model;
            options.Temperature = ReadDouble(section["Temperature"], options.Temperature);
            options.TimeoutSeconds = ReadInt(section["TimeoutSeconds"], options.TimeoutSeconds);
        }

        options.ApiKey = ReadSetting(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"), options.ApiKey);
        options.Model = ReadSetting(Environment.GetEnvironmentVariable("DASHSCOPE_MODEL_NAME"), options.Model) ?? options.Model;

        return options;
    }

    private static string? ReadSetting(string? value, string? currentValue)
    {
        return string.IsNullOrWhiteSpace(value) ? currentValue : value;
    }

    private static double ReadDouble(string? value, double currentValue)
    {
        return double.TryParse(value, out double parsed) ? parsed : currentValue;
    }

    private static int ReadInt(string? value, int currentValue)
    {
        return int.TryParse(value, out int parsed) ? parsed : currentValue;
    }

    private static Assembly[] ResolveAssemblies(Assembly[] moduleAssemblies)
    {
        Assembly[] defaultAssemblies =
        [
            typeof(CapabilityRegistry).Assembly,
            typeof(QueryOrderBusinessAction).Assembly,
            typeof(WorkflowDefinitionRegistry).Assembly
        ];

        return defaultAssemblies
            .Concat(moduleAssemblies)
            .Distinct()
            .ToArray();
    }

    private static void RegisterImplementations<TService>(IServiceCollection services, IEnumerable<Assembly> assemblies)
        where TService : class
    {
        Type serviceType = typeof(TService);
        IEnumerable<Type> implementationTypes = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => serviceType.IsAssignableFrom(type))
            .Select(type => type.AsType())
            .Distinct();

        foreach (Type implementationType in implementationTypes)
        {
            services.AddSingleton(serviceType, implementationType);
        }
    }
}
