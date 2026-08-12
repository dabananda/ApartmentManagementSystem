using Microsoft.Extensions.DependencyInjection;

namespace ApartmentManagementSystem.Application.Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetRequiredService(handlerType);

        var method = handlerType.GetMethod("Handle");
        if (method == null)
        {
            throw new InvalidOperationException($"Handler for {requestType.Name} does not implement Handle method.");
        }

        return (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        var handler = _serviceProvider.GetRequiredService(handlerType);

        var method = handlerType.GetMethod("Handle");
        if (method == null)
        {
            throw new InvalidOperationException($"Handler for {requestType.Name} does not implement Handle method.");
        }

        return (Task)method.Invoke(handler, new object[] { request, cancellationToken })!;
    }
}
