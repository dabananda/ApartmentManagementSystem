using AMS.Domain.Entities;

namespace AMS.Application.Interfaces.Payments;

public interface IPaymentRepository
{

    Task<TenantBill?> GetTenantBillForCheckoutAsync(Guid billId, string tenantUserId, CancellationToken cancellationToken = default);
    Task<decimal> GetPaidAmountForTenantBillAsync(Guid billId, CancellationToken cancellationToken = default);

    Task<ExpenseAllocation?> GetExpenseAllocationForCheckoutAsync(Guid commonBillId, string ownerId, CancellationToken cancellationToken = default);
    Task<decimal> GetPaidAmountForAllocationAsync(Guid commonBillId, string ownerId, CancellationToken cancellationToken = default);

    Task<(bool success, TenantPayment? payment)> ProcessTenantPaymentFromWebhookAsync(Guid billId, string tenantUserId, decimal amountReceived, string paymentRef, CancellationToken cancellationToken = default);
    Task<(bool success, ExpenseAllocationPayment? payment)> ProcessOwnerPaymentFromWebhookAsync(Guid commonBillId, string ownerId, decimal amountReceived, string paymentRef, CancellationToken cancellationToken = default);
}
