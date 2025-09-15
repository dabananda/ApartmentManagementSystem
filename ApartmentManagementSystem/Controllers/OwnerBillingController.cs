using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Owner;
using ApartmentManagementSystem.ViewModels.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class OwnerBillingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailSender _email;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;

        public OwnerBillingController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> users,
            IEmailSender email,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor)
        {
            _db = db;
            _users = users;
            _email = email;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
        }

        // GET: /OwnerBilling/Index/{buildingId}
        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (me?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            // Owners -> names
            var owners = await _db.Flats.AsNoTracking()
                .Include(f => f.Owner)
                .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
                .Select(f => new { f.OwnerId, Name = f.Owner!.Fullname })
                .Distinct()
                .ToListAsync();

            // Flats per owner
            var flatsCsv = await _db.Flats.AsNoTracking()
                .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
                .GroupBy(f => f.OwnerId!)
                .Select(g => new { OwnerId = g.Key, Csv = string.Join(", ", g.OrderBy(x => x.FlatNumber).Select(x => x.FlatNumber)) })
                .ToDictionaryAsync(x => x.OwnerId, x => x.Csv);

            // Totals per owner
            // 1) Allocated totals per owner
            var allocAggList = await _db.ExpenseAllocations.AsNoTracking()
                .Include(a => a.CommonBill)
                .Where(a => a.CommonBill!.BuildingId == buildingId)
                .GroupBy(a => a.OwnerId)
                .Select(g => new
                {
                    OwnerId = g.Key,
                    Alloc = g.Sum(x => x.AmountDue)
                })
                .ToListAsync();
            var allocAgg = allocAggList.ToDictionary(x => x.OwnerId, x => x.Alloc);

            // 2) Paid totals per owner (join payments → allocations to get OwnerId)
            var paidAggList = await _db.ExpenseAllocationPayments.AsNoTracking()
                .Join(
                    _db.ExpenseAllocations.Include(a => a.CommonBill)
                        .Where(a => a.CommonBill!.BuildingId == buildingId),
                    p => p.ExpenseAllocationId,
                    a => a.Id,
                    (p, a) => new { a.OwnerId, p.Amount }
                )
                .GroupBy(x => x.OwnerId)
                .Select(g => new
                {
                    OwnerId = g.Key!,
                    Paid = g.Sum(x => x.Amount)
                })
                .ToListAsync();
            var paidAgg = paidAggList.ToDictionary(x => x.OwnerId, x => x.Paid);

            // Build rows
            var rows = owners.Select(o => new OwnerBillingRow
            {
                OwnerId = o.OwnerId!,
                OwnerName = string.IsNullOrWhiteSpace(o.Name) ? "(no name)" : o.Name!,
                FlatsCsv = flatsCsv.TryGetValue(o.OwnerId!, out var csv) ? csv : "",
                TotalAllocated = allocAgg.TryGetValue(o.OwnerId!, out var alloc) ? alloc : 0m,
                TotalPaid = paidAgg.TryGetValue(o.OwnerId!, out var paid) ? paid : 0m
            })
            .OrderBy(r => r.OwnerName)
            .ToList();

            ViewData["BuildingId"] = buildingId;

            return View(rows);
        }

        // GET: /OwnerBilling/View/{ownerId}
        public async Task<IActionResult> View(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (me?.BuildingId == null && !User.IsInRole("SuperAdmin")) return Forbid();

            // All allocations for this owner across their building(s); restrict for President
            var q = _db.ExpenseAllocations
                .Include(a => a.CommonBill)
                .Include(a => a.Payments)
                .Where(a => a.OwnerId == ownerId);

            if (User.IsInRole("President"))
                q = q.Where(a => a.CommonBill!.BuildingId == me!.BuildingId);

            var allocations = await q.AsNoTracking().ToListAsync();
            if (allocations.Count == 0) return NotFound("No bills found for this owner.");

            var buildingId = allocations.First().CommonBill!.BuildingId;
            var owner = await _users.FindByIdAsync(ownerId);

            var items = allocations
                .OrderByDescending(a => a.CommonBill!.BillDate)
                .Select(a => new OwnerBillItem
                {
                    CommonBillId = a.CommonBillId,
                    Title = a.CommonBill!.Name,
                    BillDate = a.CommonBill!.BillDate,
                    Allocated = a.AmountDue,
                    Paid = a.Payments.Sum(p => p.Amount)
                })
                .ToList();

            var page = new OwnerBillsPage
            {
                OwnerId = ownerId,
                OwnerName = owner?.Fullname ?? "(no name)",
                BuildingId = buildingId,
                Bills = items
            };

            // ---- Payment history for this owner (scoped to building for President) ----
            var paymentsQuery = _db.ExpenseAllocationPayments
                .Join(_db.ExpenseAllocations.Include(a => a.CommonBill),
                      p => p.ExpenseAllocationId,
                      a => a.Id,
                      (p, a) => new { p, a });

            if (User.IsInRole("President"))
            {
                var bld = buildingId; // computed earlier from allocations
                paymentsQuery = paymentsQuery.Where(z => z.a.OwnerId == ownerId && z.a.CommonBill!.BuildingId == bld);
            }
            else
            {
                paymentsQuery = paymentsQuery.Where(z => z.a.OwnerId == ownerId);
            }

            var history = await paymentsQuery
                .OrderByDescending(z => z.p.PaymentDate)
                .ThenByDescending(z => z.a.CommonBill!.BillDate)
                .Select(z => new OwnerPaymentRecord
                {
                    PaymentId = z.p.Id,
                    PaymentDate = z.p.PaymentDate,
                    BillTitle = z.a.CommonBill!.Name,
                    BillDate = z.a.CommonBill!.BillDate,
                    Amount = z.p.Amount,
                    Reference = z.p.Reference
                })
                .ToListAsync();

            page.History = history;

            return View(page);
        }

        // POST: /OwnerBilling/Pay
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(RecordOwnerPaymentVM vm)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            var bill = await _db.CommonBills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == vm.CommonBillId);
            if (bill == null) return NotFound();
            if (User.IsInRole("President") && me.BuildingId != bill.BuildingId) return Forbid();

            // Idempotency: short-circuit if already processed
            if (!string.IsNullOrWhiteSpace(vm.IdempotencyKey))
            {
                var exists = await _db.ExpenseAllocationPayments
                    .AsNoTracking()
                    .AnyAsync(p => p.IdempotencyKey == vm.IdempotencyKey);
                if (exists)
                {
                    TempData["Success"] = "Payment recorded.";
                    return RedirectToAction(nameof(View), new { ownerId = vm.OwnerId });
                }
            }

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var allocs = await _db.ExpenseAllocations
                .Include(a => a.CommonBill)
                .Where(a => a.CommonBillId == vm.CommonBillId && a.OwnerId == vm.OwnerId)
                .OrderBy(a => a.Id)
                .ToListAsync();
            if (allocs.Count == 0) return NotFound("No allocation found for this owner & bill.");

            var totalDueNow = 0m;
            foreach (var a in allocs)
            {
                var paid = await _db.ExpenseAllocationPayments
                    .Where(p => p.ExpenseAllocationId == a.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;
                totalDueNow += Math.Max(0, a.AmountDue - paid);
            }

            if (totalDueNow <= 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No due for this owner on the selected bill.";
                return RedirectToAction(nameof(View), new { ownerId = vm.OwnerId });
            }

            var remaining = Math.Min(vm.Amount, totalDueNow);
            var created = new List<ExpenseAllocationPayment>();

            foreach (var a in allocs)
            {
                if (remaining <= 0) break;

                var paid = await _db.ExpenseAllocationPayments
                    .Where(p => p.ExpenseAllocationId == a.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var due = a.AmountDue - paid;
                if (due <= 0) continue;

                var take = Math.Min(due, remaining);
                var entity = new ExpenseAllocationPayment
                {
                    ExpenseAllocationId = a.Id,
                    Amount = take,
                    PaymentDate = vm.PaymentDate,
                    Reference = vm.Reference,
                    CommonBillId = vm.CommonBillId,
                    OwnerId = vm.OwnerId,
                    IdempotencyKey = string.IsNullOrWhiteSpace(vm.IdempotencyKey) ? null : vm.IdempotencyKey,
                    Gateway = string.IsNullOrWhiteSpace(vm.IdempotencyKey) ? PaymentGateway.None : PaymentGateway.Stripe,
                    Status = PaymentStatus.Succeeded
                };
                await _db.ExpenseAllocationPayments.AddAsync(entity);
                created.Add(entity);

                // keep legacy flags for compatibility
                if (take == due)
                {
                    a.IsPaid = true;
                    a.PaymentDate = vm.PaymentDate;
                }

                remaining -= take;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await SendPaymentEmailAsync(vm.OwnerId, created);

            TempData["Success"] = (vm.Amount > totalDueNow)
                ? $"Payment recorded (clamped to {totalDueNow:C})."
                : "Payment recorded.";
            return RedirectToAction(nameof(View), new { ownerId = vm.OwnerId });
        }

        // POST: /OwnerBilling/PayAll
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAll(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return BadRequest();

            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var q = _db.ExpenseAllocations
                .Include(a => a.CommonBill)
                .Where(a => a.OwnerId == ownerId);

            if (User.IsInRole("President"))
                q = q.Where(a => a.CommonBill!.BuildingId == me.BuildingId);

            var allocs = await q.ToListAsync();
            if (allocs.Count == 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No bills found for this owner.";
                return RedirectToAction(nameof(View), new { ownerId });
            }

            var created = new List<ExpenseAllocationPayment>();
            var today = DateTime.Today;

            foreach (var a in allocs.OrderBy(x => x.CommonBill!.BillDate).ThenBy(x => x.Id))
            {
                var paid = await _db.ExpenseAllocationPayments
                    .Where(p => p.ExpenseAllocationId == a.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var due = a.AmountDue - paid;
                if (due <= 0) continue;

                var entity = new ExpenseAllocationPayment
                {
                    ExpenseAllocationId = a.Id,
                    Amount = due,
                    PaymentDate = today,
                    Reference = "PayAll",
                    CommonBillId = a.CommonBillId,
                    OwnerId = ownerId,
                    Status = PaymentStatus.Succeeded
                };
                await _db.ExpenseAllocationPayments.AddAsync(entity);
                created.Add(entity);

                a.IsPaid = true;
                a.PaymentDate = today;
            }

            if (created.Count == 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Nothing due to pay.";
                return RedirectToAction(nameof(View), new { ownerId });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await SendPaymentEmailAsync(ownerId, created);

            var total = created.Sum(x => x.Amount);
            TempData["Success"] = $"All outstanding dues paid ({total:C}).";
            return RedirectToAction(nameof(View), new { ownerId });
        }

        // POST: /OwnerBilling/FullPay  (one bill → full)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FullPay(string ownerId, Guid commonBillId)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || commonBillId == Guid.Empty)
                return BadRequest();

            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var q = _db.ExpenseAllocations
                .Include(a => a.CommonBill)
                .Where(a => a.OwnerId == ownerId && a.CommonBillId == commonBillId);

            if (User.IsInRole("President"))
                q = q.Where(a => a.CommonBill!.BuildingId == me.BuildingId);

            var allocs = await q.ToListAsync();
            if (allocs.Count == 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No allocations found for this owner and bill.";
                return RedirectToAction(nameof(View), new { ownerId });
            }

            decimal totalDueNow = 0m;
            foreach (var a in allocs)
            {
                var paid = await _db.ExpenseAllocationPayments
                    .Where(p => p.ExpenseAllocationId == a.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;
                totalDueNow += Math.Max(0, a.AmountDue - paid);
            }
            if (totalDueNow <= 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "This bill has no due amount.";
                return RedirectToAction(nameof(View), new { ownerId });
            }

            var created = new List<ExpenseAllocationPayment>();
            var today = DateTime.Today;

            foreach (var a in allocs.OrderBy(x => x.Id))
            {
                var paid = await _db.ExpenseAllocationPayments
                    .Where(p => p.ExpenseAllocationId == a.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                var due = a.AmountDue - paid;
                if (due <= 0) continue;

                var entity = new ExpenseAllocationPayment
                {
                    ExpenseAllocationId = a.Id,
                    Amount = due,
                    PaymentDate = today,
                    Reference = "FullPay",
                    CommonBillId = a.CommonBillId,
                    OwnerId = ownerId,
                    Status = PaymentStatus.Succeeded
                };
                await _db.ExpenseAllocationPayments.AddAsync(entity);
                created.Add(entity);

                a.IsPaid = true;
                a.PaymentDate = today;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await SendPaymentEmailAsync(ownerId, created);

            TempData["Success"] = "Bill fully paid.";
            return RedirectToAction(nameof(View), new { ownerId });
        }

        // GET: /OwnerBilling/Receipt/{id}
        // BUGFIX: This previously tried to load TenantPayment by id; now correctly uses ExpenseAllocationPayment.
        public async Task<IActionResult> Receipt(Guid id)
        {
            var pay = await _db.ExpenseAllocationPayments
                .Where(x => x.Id == id)
                .Join(_db.ExpenseAllocations.Include(a => a.CommonBill).Include(a => a.Owner),
                      p => p.ExpenseAllocationId,
                      a => a.Id,
                      (p, a) => new { Payment = p, Allocation = a, Bill = a.CommonBill!, Owner = a.Owner! })
                .FirstOrDefaultAsync();

            if (pay == null) return NotFound();

            // Building scoping for President
            var me = await _users.GetUserAsync(User);
            if (User.IsInRole("President") && me?.BuildingId != pay.Bill.BuildingId) return Forbid();

            var vm = new ReceiptViewModel
            {
                Id = pay.Payment.Id,
                ReceiptNo = "RC-" + pay.Payment.Id.ToString().Substring(0, 8).ToUpper(),
                PaidOn = pay.Payment.PaymentDate,
                OwnerName = pay.Owner.Fullname ?? pay.Owner.UserName!,
                OwnerEmail = pay.Owner.Email,
                BillTitle = pay.Bill.Name,
                BillDate = pay.Bill.BillDate,
                Amount = pay.Payment.Amount,
                Reference = pay.Payment.Reference,
                BuildingName = pay.Bill.Building?.Name ?? "(building)",
                FlatNumber = "-" // not applicable here
            };
            return View("~/Views/Shared/Receipt.cshtml", vm);
        }

        private string AbsoluteUrl(string action, object? routeValues = null)
        {
            var actionContext = _actionContextAccessor.ActionContext!;
            var urlHelper = _urlHelperFactory.GetUrlHelper(actionContext);
            var rel = urlHelper.Action(action, "OwnerBilling", routeValues, actionContext.HttpContext.Request.Scheme)!;
            return rel;
        }

        private async Task SendPaymentEmailAsync(string ownerId, IEnumerable<ExpenseAllocationPayment> payments)
        {
            var owner = await _users.FindByIdAsync(ownerId);
            if (owner == null || string.IsNullOrWhiteSpace(owner.Email)) return;

            var list = payments.ToList();
            if (list.Count == 0) return;

            var rows = new System.Text.StringBuilder();
            foreach (var p in list)
            {
                var allocation = await _db.ExpenseAllocations
                    .Include(a => a.CommonBill)
                    .FirstAsync(a => a.Id == p.ExpenseAllocationId);

                var receiptUrl = AbsoluteUrl(nameof(Receipt), new { id = p.Id });
                rows.AppendLine($@"
            <tr>
                <td>{allocation.CommonBill!.Name}</td>
                <td>{allocation.CommonBill.BillDate:yyyy-MM-dd}</td>
                <td style=""text-align:right"">{p.Amount:C}</td>
                <td>{(string.IsNullOrWhiteSpace(p.Reference) ? "-" : p.Reference)}</td>
                <td><a href=""{receiptUrl}"">Receipt</a></td>
            </tr>");
            }

            var total = list.Sum(x => x.Amount);

            var html = $@"
        <p>Hello {System.Net.WebUtility.HtmlEncode(owner.Fullname ?? owner.UserName)},</p>
        <p>We’ve recorded your payment{(list.Count > 1 ? "s" : "")}:</p>
        <table cellpadding=""6"" cellspacing=""0"" border=""1"" style=""border-collapse:collapse;"">
            <thead><tr>
                <th>Bill</th><th>Bill Date</th><th>Amount</th><th>Reference</th><th>Receipt</th>
            </tr></thead>
            <tbody>{rows}</tbody>
            <tfoot><tr><td colspan=""2"" style=""text-align:right""><strong>Total</strong></td><td style=""text-align:right""><strong>{total:C}</strong></td><td colspan=""2""></td></tr></tfoot>
        </table>
        <p>Thank you.</p>";

            await _email.SendEmailAsync(owner.Email!, "Payment receipt", html);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailReceipt(Guid id)
        {
            var pay = await _db.ExpenseAllocationPayments.FindAsync(id);
            if (pay == null) return NotFound();

            var alloc = await _db.ExpenseAllocations.Include(a => a.CommonBill).FirstAsync(a => a.Id == pay.ExpenseAllocationId);

            var me = await _users.GetUserAsync(User);
            if (User.IsInRole("President") && me?.BuildingId != alloc.CommonBill!.BuildingId) return Forbid();

            await SendPaymentEmailAsync(pay.OwnerId, new[] { pay });
            TempData["Success"] = "Receipt email sent.";
            return RedirectToAction(nameof(View), new { ownerId = pay.OwnerId });
        }
    }
}
