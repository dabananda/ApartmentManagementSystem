using ApartmentManagementSystem.Application.Mediator;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ApartmentManagementSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMediator, Mediator.Mediator>();

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

        // Register all application services automatically
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Service") && !t.IsAbstract && !t.IsInterface);

        foreach (var serviceType in serviceTypes)
        {
            var interfaceType = serviceType.GetInterfaces().FirstOrDefault(i => i.Name == $"I{serviceType.Name}");
            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, serviceType);
            }
        }

        return services;
    }
}
