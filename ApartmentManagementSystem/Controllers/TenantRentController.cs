using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Tenant;
using ApartmentManagementSystem.ViewModels.TenantPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net.Mail;
using System.Net.Sockets;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class TenantRentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailSender _email;
        private readonly ILogger<TenantRentController> _log;

        public TenantRentController(ApplicationDbContext db, UserManager<ApplicationUser> users, IEmailSender email, ILogger<TenantRentController> log)
        {
            _db = db; _users = users; _email = email; _log = log;
        }

        public async Task<IActionResult> List()
        {
            var me = await _users.GetUserAsync(User);

            var q = _db.TenantAssignments.AsNoTracking()
                .Include(a => a.Flat)
                .Include(a => a.TenantUser)
                .Where(a => a.EndDate == null);

            if (User.IsInRole("Owner"))
                q = q.Where(a => a.Flat!.OwnerId == me!.Id);

            var tenants = await q
                .GroupBy(a => new
                {
                    a.TenantUserId,
                    Fullname = a.TenantUser!.Fullname,
                    UserName = a.TenantUser!.UserName,
                    Email = a.TenantUser!.Email
                })
                .Select(g => new TenantRentListRow
                {
                    TenantUserId = g.Key.TenantUserId!,
                    Name = (g.Key.Fullname ?? g.Key.UserName)!,
                    Email = g.Key.Email!,
                    BuildingId = g.Max(x => x.Flat!.BuildingId)
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(tenants);
        }

        public async Task<IActionResult> View(string tenantUserId)
        {
            var me = await _users.GetUserAsync(User);

            var allowed = await _db.TenantAssignments
                .Include(a => a.Flat)
                .AnyAsync(a => a.TenantUserId == tenantUserId &&
                               (!User.IsInRole("Owner") || a.Flat!.OwnerId == me!.Id));
            if (!allowed) return Forbid();

            await EnsureCurrentMonthBillsForTenantAsync(tenantUserId);

            var bills = await _db.TenantBills
                .Include(b => b.Payments)
                .Include(b => b.Flat)
                .Where(b => b.TenantUserId == tenantUserId)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
            if (bills.Count == 0) return NotFound("No bills.");

            var tenant = await _users.FindByIdAsync(tenantUserId);

            var page = new TenantBillsPage
            {
                TenantUserId = tenantUserId,
                TenantName = tenant!.Fullname ?? tenant.UserName ?? "(no name)",
                Email = tenant.Email ?? "",
                BuildingId = bills.First().Flat!.BuildingId,
                Bills = bills.Select(b => new TenantBillRow
                {
                    BillId = b.Id,
                    Title = b.Title,
                    BillDate = b.BillDate,
                    Amount = b.Amount,
                    Paid = b.Payments.Sum(p => p.Amount)
                }).ToList()
            };

            var history = await _db.TenantPayments
                .Include(p => p.TenantBill)
                .Where(p => p.TenantBill!.TenantUserId == tenantUserId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new TenantPaymentRecord
                {
                    PaymentId = p.Id,
                    PaymentDate = p.PaymentDate,
                    BillTitle = p.TenantBill!.Title,
                    BillDate = p.TenantBill.BillDate,
                    Amount = p.Amount,
                    Reference = p.Reference
                }).ToListAsync();

            ViewData["History"] = history;
            return View(page);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(RecordTenantPaymentVM vm)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var bill = await _db.TenantBills
                .Include(b => b.Flat)
                .FirstOrDefaultAsync(b => b.Id == vm.TenantBillId);
            if (bill == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (User.IsInRole("Owner") && bill.Flat!.OwnerId != me!.Id) return Forbid();

            if (!string.IsNullOrWhiteSpace(vm.IdempotencyKey))
            {
                var existing = await _db.TenantPayments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == vm.IdempotencyKey);
                if (existing != null)
                {
                    await tx.CommitAsync();
                    TempData["Success"] = "Payment recorded.";
                    return RedirectToAction(nameof(View), new { tenantUserId = bill.TenantUserId });
                }
            }

            var paidNow = await _db.TenantPayments
                .Where(p => p.TenantBillId == bill.Id)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var dueNow = bill.Amount - paidNow;
            if (dueNow <= 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No due on this bill.";
                return RedirectToAction(nameof(View), new { tenantUserId = bill.TenantUserId });
            }

            var take = Math.Min(vm.Amount, dueNow);
            if (take <= 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Nothing to pay.";
                return RedirectToAction(nameof(View), new { tenantUserId = bill.TenantUserId });
            }

            var entity = new TenantPayment
            {
                TenantBillId = bill.Id,
                Amount = take,
                PaymentDate = vm.PaymentDate,
                Reference = vm.Reference,
                IdempotencyKey = string.IsNullOrWhiteSpace(vm.IdempotencyKey) ? null : vm.IdempotencyKey,
                Gateway = string.IsNullOrWhiteSpace(vm.IdempotencyKey) ? PaymentGateway.None : PaymentGateway.Stripe,
                Status = PaymentStatus.Succeeded
            };

            await _db.TenantPayments.AddAsync(entity);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await SendTenantPaymentEmail(bill.TenantUserId, new[] { entity });

            TempData["Success"] = take < vm.Amount
                ? $"Payment recorded (clamped to {take:C} to avoid overpay)."
                : "Payment recorded.";
            return RedirectToAction(nameof(View), new { tenantUserId = bill.TenantUserId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FullPay(Guid billId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var bill = await _db.TenantBills
                .Include(b => b.Flat)
                .FirstOrDefaultAsync(b => b.Id == billId);
            if (bill == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (User.IsInRole("Owner") && bill.Flat!.OwnerId != me!.Id) return Forbid();

            var paidNow = await _db.TenantPayments
                .Where(p => p.TenantBillId == bill.Id)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var dueNow = bill.Amount - paidNow;
            if (dueNow <= 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No due on this bill.";
                return RedirectToAction(nameof(View), new { tenantUserId = bill.TenantUserId });
            }

            var entity = new TenantPayment
            {
                TenantBillId = bill.Id,
                Amount = dueNow,
                PaymentDate = DateTime.Today,
                Reference = "FullPay",
                Status = PaymentStatus.Succeeded
            };

            await _db.TenantPayments.AddAsync(entity);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await SendTenantPaymentEmail(bill.TenantUserId, new[] { entity });

            TempData["Success"] = "Bill fully paid.";
            return RedirectToAction(nameof(View), new { tenantUserId = bill.TenantUserId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAll(string tenantUserId)
        {
            var me = await _users.GetUserAsync(User);

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var bills = await _db.TenantBills
                .Include(b => b.Flat)
                .Where(b => b.TenantUserId == tenantUserId &&
                            (!User.IsInRole("Owner") || b.Flat!.OwnerId == me!.Id))
                .OrderBy(b => b.BillDate)
                .ToListAsync();

            if (bills.Count == 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No bills found.";
                return RedirectToAction(nameof(List));
            }

            var created = new List<TenantPayment>();
            foreach (var b in bills)
            {
                var paidNow = await _db.TenantPayments
                    .Where(p => p.TenantBillId == b.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var due = b.Amount - paidNow;
                if (due <= 0) continue;

                var e = new TenantPayment
                {
                    TenantBillId = b.Id,
                    Amount = due,
                    PaymentDate = DateTime.Today,
                    Reference = "PayAll",
                    Status = PaymentStatus.Succeeded
                };
                await _db.TenantPayments.AddAsync(e);
                created.Add(e);
            }

            if (created.Count == 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Nothing due to pay.";
                return RedirectToAction(nameof(View), new { tenantUserId });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await SendTenantPaymentEmail(tenantUserId, created);

            TempData["Success"] = "All outstanding dues paid.";
            return RedirectToAction(nameof(View), new { tenantUserId });
        }

        public async Task<IActionResult> Receipt(Guid id)
        {
            var p = await _db.TenantPayments
                .Include(x => x.TenantBill)!.ThenInclude(b => b.Flat)
                .Include(x => x.TenantBill)!.ThenInclude(b => b.TenantUser)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (User.IsInRole("Owner") && p.TenantBill!.Flat!.OwnerId != me!.Id) return Forbid();

            var payment = await _db.TenantPayments
                .Include(p => p.TenantBill)!.ThenInclude(b => b.Flat)!.ThenInclude(f => f.Building)
                .Include(p => p.TenantBill)!.ThenInclude(b => b.TenantUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null) return NotFound();

            var vm = new ReceiptViewModel
            {
                Id = payment.Id,
                ReceiptNo = "RC-" + payment.Id.ToString().Substring(0, 8).ToUpper(),
                PaidOn = payment.PaymentDate,
                OwnerName = payment.TenantBill!.TenantUser!.Fullname ?? payment.TenantBill!.TenantUser!.UserName!,
                OwnerEmail = payment.TenantBill!.TenantUser!.Email,
                BillTitle = payment.TenantBill.Title,
                BillDate = payment.TenantBill.BillDate,
                Amount = payment.Amount,
                Reference = payment.Reference,
                BuildingName = payment.TenantBill!.Flat!.Building!.Name,
                FlatNumber = payment.TenantBill!.Flat!.FlatNumber
            };
            return View("~/Views/Shared/Receipt.cshtml", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailReceipt(Guid id)
        {
            var p = await _db.TenantPayments.Include(x => x.TenantBill)!.ThenInclude(b => b.Flat).FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (User.IsInRole("Owner") && p.TenantBill!.Flat!.OwnerId != me!.Id) return Forbid();

            await SendTenantPaymentEmail(p.TenantBill!.TenantUserId, new[] { p });
            TempData["Success"] = "Receipt email sent.";
            return RedirectToAction(nameof(View), new { tenantUserId = p.TenantBill!.TenantUserId });
        }

        private async Task SendTenantPaymentEmail(string tenantUserId, IEnumerable<TenantPayment> payments)
        {
            var user = await _users.FindByIdAsync(tenantUserId);
            if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

            var list = payments.ToList();
            if (list.Count == 0) return;

            try
            {
                var billIds = list.Select(x => x.TenantBillId).ToList();
                var bills = await _db.TenantBills
                    .AsNoTracking()
                    .Include(x => x.Flat)
                    .Where(x => billIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);

                var rows = new System.Text.StringBuilder();
                foreach (var p in list)
                {
                    var b = bills[p.TenantBillId];
                    rows.AppendLine($@"<tr>
                    <td>{b.Title}</td><td>{b.BillDate:yyyy-MM-dd}</td>
                    <td style=""text-align:right"">{p.Amount:C}</td><td>{(string.IsNullOrWhiteSpace(p.Reference) ? "-" : p.Reference)}</td>
                </tr>");
                }

                var total = list.Sum(x => x.Amount);
                var html = $@"
            <p>Hello {System.Net.WebUtility.HtmlEncode(user.Fullname ?? user.UserName)},</p>
            <p>We’ve recorded your rent payment{(list.Count > 1 ? "s" : "")}:</p>
            <table cellpadding=""6"" border=""1"" style=""border-collapse:collapse;"">
                <thead><tr><th>Bill</th><th>Bill Date</th><th>Amount</th><th>Reference</th></tr></thead>
                <tbody>{rows}</tbody>
                <tfoot><tr><td colspan=""2"" style=""text-align:right""><strong>Total</strong></td>
                <td style=""text-align:right""><strong>{total:C}</strong></td><td></td></tr></tfoot>
            </table>
            <p>Thank you.</p>";

                await _email.SendEmailAsync(user.Email!, "Rent payment receipt", html);
            }
            catch (SmtpException ex)
            {
                _log.LogWarning(ex, "SMTP failed while emailing rent receipt to {Email}. Payment saved.", user.Email);
                TempData["Warning"] = "Payment saved, but the receipt email could not be sent (SMTP issue).";
            }
            catch (SocketException ex)
            {
                _log.LogWarning(ex, "Socket error while emailing rent receipt to {Email}. Payment saved.", user.Email);
                TempData["Warning"] = "Payment saved, but the receipt email could not be sent (network issue).";
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error while emailing rent receipt to {Email}. Payment saved.", user.Email);
                TempData["Warning"] = "Payment saved, but sending the receipt failed.";
            }
        }

        private async Task<int> EnsureCurrentMonthBillsForTenantAsync(string tenantUserId)
        {
            var today = DateTime.Today;
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            var activeAssignments = await _db.TenantAssignments
                .Include(a => a.Flat)
                .Where(a => a.TenantUserId == tenantUserId &&
                            (a.EndDate == null || a.EndDate >= today))
                .ToListAsync();

            if (activeAssignments.Count == 0) return 0;

            var flatIds = activeAssignments.Select(a => a.FlatId).Distinct().ToList();

            var profiles = await _db.FlatBillingProfiles
                .Where(p => flatIds.Contains(p.FlatId) && p.IsActive)
                .ToListAsync();

            if (profiles.Count == 0) return 0;

            var existingBills = await _db.TenantBills
                .Where(b => b.TenantUserId == tenantUserId && b.BillDate == firstOfMonth)
                .Select(b => new { b.FlatId })
                .ToListAsync();
            var existingFlatIds = existingBills.Select(b => b.FlatId).ToHashSet();

            int created = 0;
            foreach (var prof in profiles)
            {
                var assignment = activeAssignments
                    .Where(a => a.FlatId == prof.FlatId)
                    .OrderByDescending(a => a.StartDate)
                    .FirstOrDefault();
                if (assignment == null) continue;

                var startMonth = new DateTime(assignment.StartDate.Year, assignment.StartDate.Month, 1);
                if (startMonth > firstOfMonth) continue;

                if (existingFlatIds.Contains(prof.FlatId)) continue;

                await _db.TenantBills.AddAsync(new TenantBill
                {
                    FlatId = prof.FlatId,
                    TenantUserId = tenantUserId,
                    Title = string.IsNullOrWhiteSpace(prof.Title) ? "Monthly Rent" : prof.Title,
                    BillDate = firstOfMonth,
                    Amount = prof.MonthlyAmount
                });
                created++;
            }

            if (created > 0) await _db.SaveChangesAsync();
            return created;
        }
    }
}
