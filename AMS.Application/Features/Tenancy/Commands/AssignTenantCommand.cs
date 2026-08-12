using AMS.Application.Interfaces.Tenancy;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Tenancy.Commands;

public record AssignTenantCommand(Guid FlatId, string TenantId) : IRequest;

public class AssignTenantCommandHandler(ITenantAssignmentRepository repo)
    : IRequestHandler<AssignTenantCommand>
{
    public Task Handle(AssignTenantCommand request, CancellationToken cancellationToken = default)
        => repo.ReplaceAsync(request.FlatId, request.TenantId);
}
