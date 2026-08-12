using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Sends payment confirmation emails for both tenant rent payments and owner common bill payments.
/// Unifies identical email logic that was previously duplicated across TenantRentController,
/// OwnerBillingController and PaymentsController.
/// </summary>
public interface IPaymentEmailService
{
    Task SendTenantPaymentEmailAsync(string tenantUserId, IEnumerable<TenantPayment> payments, CancellationToken cancellationToken = default);
    Task SendOwnerPaymentEmailAsync(string ownerUserId, IEnumerable<ExpenseAllocationPayment> payments, Func<Guid, string>? getReceiptUrl = null, CancellationToken cancellationToken = default);
}
