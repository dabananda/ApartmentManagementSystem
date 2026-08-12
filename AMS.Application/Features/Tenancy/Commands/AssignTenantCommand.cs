using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;

namespace AMS.Application.Features.Tenancy.Commands;

public record AssignTenantCommand(Guid FlatId, string TenantId) : IRequest;

public class AssignTenantCommandHandler(ITenantAssignmentRepository repo)
    : IRequestHandler<AssignTenantCommand>
{
    public Task Handle(AssignTenantCommand request, CancellationToken cancellationToken = default)
        => repo.ReplaceAsync(request.FlatId, request.TenantId);
}
