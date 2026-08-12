using AMS.Domain.Entities;

namespace AMS.Infrastructure.Services;

public interface IPaymentEmailService
{
    Task SendTenantPaymentEmailAsync(string tenantUserId, IEnumerable<TenantPayment> payments, CancellationToken cancellationToken = default);
    Task SendOwnerPaymentEmailAsync(string ownerUserId, IEnumerable<ExpenseAllocationPayment> payments, Func<Guid, string>? getReceiptUrl = null, CancellationToken cancellationToken = default);
}
