using System.Reflection;
using AMS.Application.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace AMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped, not Singleton: handlers (and the repositories/DbContext they depend on)
        // are registered Scoped. A Singleton Mediator would capture the root IServiceProvider
        // and resolve scoped services from it, either throwing under scope validation or,
        // worse, silently sharing a single DbContext instance across all requests.
        services.AddScoped<IMediator, Mediator.Mediator>();

        var assembly = Assembly.GetExecutingAssembly();

        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))));

        foreach (var handlerType in handlerTypes)
        {
            var interfaceTypes = handlerType.GetInterfaces().Where(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)));

            foreach (var interfaceType in interfaceTypes)
            {
                services.AddTransient(interfaceType, handlerType);
            }
        }

        // Note: no *Service reflection scan here. This assembly defines only service
        // *interfaces* (e.g. IStripePaymentService); implementations live in
        // AMS.Infrastructure and are registered explicitly in AMS.Infrastructure's
        // DependencyInjection, which is the single source of truth for those bindings.

        return services;
    }
}
