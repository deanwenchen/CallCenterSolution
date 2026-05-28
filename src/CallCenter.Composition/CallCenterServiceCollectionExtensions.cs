using System.Reflection;
using CallCenter.Application;
using CallCenter.BusinessActions;
using CallCenter.Infrastructure;
using CallCenter.Workflows;
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
        Assembly[] assemblies = ResolveAssemblies(moduleAssemblies);

        services.AddScoped<IConversationGateway, ConversationGateway>();

        services.AddSingleton<IAuthenticationService, MetadataAuthenticationService>();
        services.AddSingleton<IAuthorizationService, MetadataAuthorizationService>();
        services.AddSingleton<IRateLimiter, FixedWindowRateLimiter>();
        services.AddSingleton<IBlacklistService, MetadataBlacklistService>();
        services.AddSingleton<IAuditSink, FileAuditSink>();
        services.AddSingleton<IObservabilitySink, FileObservabilitySink>();
        services.AddSingleton<ISessionStore, FileSessionStore>();
        services.AddSingleton<IConversationContextFactory, ConversationContextFactory>();
        services.AddSingleton<IIntentRecognizer, ConfiguredIntentRecognizer>();
        services.AddSingleton<IPlanner, ConfiguredPlanner>();
        services.AddSingleton<IExternalSystemGateway, InMemoryExternalSystemGateway>();
        services.AddSingleton<IKnowledgeService, InMemoryKnowledgeService>();
        services.AddSingleton<IHumanAgentService, InMemoryHumanAgentService>();
        services.AddSingleton<InMemoryCallCenterConfiguration>();
        services.AddSingleton<IIntentDefinitionProvider>(provider => provider.GetRequiredService<InMemoryCallCenterConfiguration>());
        services.AddSingleton<IIntentCapabilityRouteProvider>(provider => provider.GetRequiredService<InMemoryCallCenterConfiguration>());
        services.AddSingleton<ICapabilityWorkflowRouteProvider>(provider => provider.GetRequiredService<InMemoryCallCenterConfiguration>());
        services.AddSingleton<IWorkflowPermissionProvider>(provider => provider.GetRequiredService<InMemoryCallCenterConfiguration>());

        RegisterImplementations<ICapability>(services, assemblies);
        RegisterImplementations<IBusinessAction>(services, assemblies);
        RegisterImplementations<IWorkflowDefinitionProvider>(services, assemblies);
        RegisterImplementations<IIntentDefinitionProvider>(services, moduleAssemblies);
        RegisterImplementations<IIntentCapabilityRouteProvider>(services, moduleAssemblies);
        RegisterImplementations<ICapabilityWorkflowRouteProvider>(services, moduleAssemblies);
        RegisterImplementations<IWorkflowPermissionProvider>(services, moduleAssemblies);

        services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();
        services.AddSingleton<IBusinessActionRegistry, BusinessActionRegistry>();
        services.AddSingleton<IWorkflowDefinitionRegistry, WorkflowDefinitionRegistry>();
        services.AddSingleton<MafWorkflowFactory>();
        services.AddSingleton<IWorkflowRuntime, MafWorkflowRuntime>();

        return services;
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
