using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;

namespace ApartmentManagementSystem.Infrastructure.Services;

/// <summary>
/// Sends payment confirmation emails for tenant rent and owner common bill payments.
/// This service consolidates identical email logic that was previously duplicated across
/// TenantRentController, OwnerBillingController, and PaymentsController.
/// </summary>
public sealed class PaymentEmailService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users,
    IEmailSender email) : IPaymentEmailService
{
    public async Task SendTenantPaymentEmailAsync(string tenantUserId, IEnumerable<TenantPayment> payments, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(tenantUserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

        var list = payments.ToList();
        if (list.Count == 0) return;

        var billIds = list.Select(x => x.TenantBillId).ToList();
        var bills = await db.TenantBills
            .AsNoTracking()
            .Include(x => x.Flat)
            .Where(x => billIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var rows = new StringBuilder();
        foreach (var p in list)
        {
            if (!bills.TryGetValue(p.TenantBillId, out var b)) continue;
            rows.AppendLine($@"<tr>
                    <td>{WebUtility.HtmlEncode(b.Title)}</td><td>{b.BillDate:yyyy-MM-dd}</td>
                    <td style=""text-align:right"">{p.Amount:C}</td><td>{(string.IsNullOrWhiteSpace(p.Reference) ? "-" : WebUtility.HtmlEncode(p.Reference))}</td>
                </tr>");
        }

        var total = list.Sum(x => x.Amount);
        var html = $@"<p>Hello {WebUtility.HtmlEncode(user.Fullname ?? user.UserName)},</p>
            <p>We've recorded your rent payment{(list.Count > 1 ? "s" : "")}:</p>
            <table cellpadding=""6"" border=""1"" style=""border-collapse:collapse;"">
                <thead><tr><th>Bill</th><th>Bill Date</th><th>Amount</th><th>Reference</th></tr></thead>
                <tbody>{rows}</tbody>
                <tfoot><tr><td colspan=""2"" style=""text-align:right""><strong>Total</strong></td>
                <td style=""text-align:right""><strong>{total:C}</strong></td><td></td></tr></tfoot>
            </table>
            <p>Thank you.</p>";

        await email.SendEmailAsync(user.Email!, "Rent payment receipt", html);
    }

    public async Task SendOwnerPaymentEmailAsync(string ownerUserId, IEnumerable<ExpenseAllocationPayment> payments, Func<Guid, string>? getReceiptUrl = null, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(ownerUserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

        var list = payments.ToList();
        if (list.Count == 0) return;

        var commonBillIds = list.Select(x => x.CommonBillId).Distinct().ToList();
        var bills = await db.CommonBills
            .AsNoTracking()
            .Include(cb => cb.Building)
            .Where(cb => commonBillIds.Contains(cb.Id))
            .ToDictionaryAsync(cb => cb.Id, cancellationToken);

        var rows = new StringBuilder();
        var hasReceipts = getReceiptUrl != null;

        foreach (var p in list)
        {
            if (!bills.TryGetValue(p.CommonBillId, out var cb)) continue;

            var receiptHtml = hasReceipts
                ? $@"<td><a href=""{getReceiptUrl!(p.Id)}"">Receipt</a></td>"
                : "";

            rows.AppendLine($@"<tr>
                    <td>{WebUtility.HtmlEncode(cb.Name)}</td><td>{cb.BillDate:yyyy-MM-dd}</td>
                    <td style=""text-align:right"">{p.Amount:C}</td><td>{(string.IsNullOrWhiteSpace(p.Reference) ? "-" : WebUtility.HtmlEncode(p.Reference))}</td>
                    {receiptHtml}
                </tr>");
        }

        var total = list.Sum(x => x.Amount);

        var thReceipt = hasReceipts ? "<th>Receipt</th>" : "";
        var colspanEmpty = hasReceipts ? @"<td colspan=""2""></td>" : "<td></td>";

        var html = $@"<p>Hello {WebUtility.HtmlEncode(user.Fullname ?? user.UserName)},</p>
            <p>We've recorded your common bill payment{(list.Count > 1 ? "s" : "")}:</p>
            <table cellpadding=""6"" border=""1"" style=""border-collapse:collapse;"">
                <thead><tr><th>Bill</th><th>Bill Date</th><th>Amount</th><th>Reference</th>{thReceipt}</tr></thead>
                <tbody>{rows}</tbody>
                <tfoot><tr><td colspan=""2"" style=""text-align:right""><strong>Total</strong></td>
                <td style=""text-align:right""><strong>{total:C}</strong></td>{colspanEmpty}</tr></tfoot>
            </table>
            <p>Thank you.</p>";

        await email.SendEmailAsync(user.Email!, "Common bill payment receipt", html);
    }
}
