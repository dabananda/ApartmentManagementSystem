using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Payments.Repositories;

public interface IPaymentRepository
{
    // Tenant Checkout details
    Task<TenantBill?> GetTenantBillForCheckoutAsync(Guid billId, string tenantUserId, CancellationToken cancellationToken = default);
    Task<decimal> GetPaidAmountForTenantBillAsync(Guid billId, CancellationToken cancellationToken = default);

    // Owner Checkout details
    Task<ExpenseAllocation?> GetExpenseAllocationForCheckoutAsync(Guid commonBillId, string ownerId, CancellationToken cancellationToken = default);
    Task<decimal> GetPaidAmountForAllocationAsync(Guid commonBillId, string ownerId, CancellationToken cancellationToken = default);

    // Webhook Processing
    Task<(bool success, TenantPayment? payment)> ProcessTenantPaymentFromWebhookAsync(Guid billId, string tenantUserId, decimal amountReceived, string paymentRef, CancellationToken cancellationToken = default);
    Task<(bool success, ExpenseAllocationPayment? payment)> ProcessOwnerPaymentFromWebhookAsync(Guid commonBillId, string ownerId, decimal amountReceived, string paymentRef, CancellationToken cancellationToken = default);
}
