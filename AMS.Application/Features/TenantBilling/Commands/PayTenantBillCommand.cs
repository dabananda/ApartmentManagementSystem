using AMS.Application.Mediator;
using AMS.Application.Interfaces.TenantBilling;
using AMS.Domain.Entities;
using AMS.Application.Features.Tenancy.DTOs;

namespace AMS.Application.Features.TenantBilling.Commands;

public record PayTenantBillCommand(RecordTenantPaymentVM Vm, string? RestrictToOwnerId) 
    : IRequest<(bool success, string message, List<TenantPayment> payments, string? tenantUserId)>;

public class PayTenantBillCommandHandler(ITenantRentRepository repository)
    : IRequestHandler<PayTenantBillCommand, (bool success, string message, List<TenantPayment> payments, string? tenantUserId)>
{
    public async Task<(bool success, string message, List<TenantPayment> payments, string? tenantUserId)> Handle(PayTenantBillCommand request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Vm.IdempotencyKey))
        {
            var exists = await repository.IdempotencyKeyExistsAsync(request.Vm.IdempotencyKey, cancellationToken);
            if (exists) return (true, "Payment recorded.", [], null);
        }

        var (created, tenantUserId) = await repository.RecordPayAsync(request.Vm, request.RestrictToOwnerId, cancellationToken);
        if (created.Count == 0) return (false, "Nothing to pay or no due on this bill.", [], tenantUserId);

        var take = created.Sum(p => p.Amount);
        var message = take < request.Vm.Amount
            ? $"Payment recorded (clamped to {take:C} to avoid overpay)."
            : "Payment recorded.";

        return (true, message, created, tenantUserId);
    }
}
