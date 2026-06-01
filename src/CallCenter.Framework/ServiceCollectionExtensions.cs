using CallCenter.Framework.EventBus;
using CallCenter.Framework.Logging;
using CallCenter.Framework.Session;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Framework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCallCenter(this IServiceCollection services)
    {
        services.AddSingleton<IBusinessEventBus, InMemoryBusinessEventBus>();
        services.AddSingleton<InMemorySessionStore>();
        services.AddSingleton<Logging.JsonlLogger>();
        return services;
    }
}
