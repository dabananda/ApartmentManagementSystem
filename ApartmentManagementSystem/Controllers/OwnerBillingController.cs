using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "President,SuperAdmin")]
    public class OwnerBillingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public OwnerBillingController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        // GET: /OwnerBilling/Index/{buildingId}
        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (me?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            // Owners -> names
            var owners = await _db.Flats
                .Include(f => f.Owner)
                .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
                .Select(f => new { f.OwnerId, Name = f.Owner!.Fullname })
                .Distinct()
                .ToListAsync();

            // Flats per owner
            var flatsCsv = await _db.Flats
                .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
                .GroupBy(f => f.OwnerId!)
                .Select(g => new { OwnerId = g.Key, Csv = string.Join(", ", g.OrderBy(x => x.FlatNumber).Select(x => x.FlatNumber)) })
                .ToDictionaryAsync(x => x.OwnerId, x => x.Csv);

            // Totals per owner
            // 1) Allocated totals per owner
            var allocAggList = await _db.ExpenseAllocations
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
            var paidAggList = await _db.ExpenseAllocationPayments
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

            // Find match allocation(s)
            var allocs = await _db.ExpenseAllocations
                .Include(a => a.Payments)
                .Where(a => a.CommonBillId == vm.CommonBillId && a.OwnerId == vm.OwnerId)
                .ToListAsync();

            if (allocs.Count == 0) return NotFound("No allocation found for this owner & bill.");

            // ✅ Compute total due across all matching allocations
            var totalDue = allocs.Sum(a => a.AmountDue - a.Payments.Sum(p => p.Amount));
            if (vm.Amount > totalDue)
            {
                TempData["Error"] = $"Amount exceeds due. Maximum payable is {totalDue:C}.";
                return RedirectToAction(nameof(View), new { ownerId = vm.OwnerId });
            }

            // Distribute amount (now guaranteed <= totalDue)
            var remaining = vm.Amount;
            foreach (var a in allocs.OrderBy(x => x.Id))
            {
                if (remaining <= 0) break;
                var already = a.Payments.Sum(p => p.Amount);
                var due = a.AmountDue - already;
                var take = Math.Min(due, remaining);
                if (take > 0)
                {
                    await _db.ExpenseAllocationPayments.AddAsync(new ExpenseAllocationPayment
                    {
                        ExpenseAllocationId = a.Id,
                        Amount = take,
                        PaymentDate = vm.PaymentDate,
                        Reference = vm.Reference,
                        CommonBillId = vm.CommonBillId,
                        OwnerId = vm.OwnerId
                    });

                    if (take == due)
                    {
                        a.IsPaid = true;
                        a.PaymentDate = vm.PaymentDate;
                    }
                    remaining -= take;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Payment recorded.";
            return RedirectToAction(nameof(View), new { ownerId = vm.OwnerId });
        }

        // POST: /OwnerBilling/PayAll
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAll(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return BadRequest();

            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            // Fetch all allocations with dues > 0 for this owner (respect building for President)
            var q = _db.ExpenseAllocations
                .Include(a => a.Payments)
                .Include(a => a.CommonBill)
                .Where(a => a.OwnerId == ownerId);

            if (User.IsInRole("President"))
                q = q.Where(a => a.CommonBill!.BuildingId == me.BuildingId);

            var allocs = await q.ToListAsync();
            if (allocs.Count == 0)
            {
                TempData["Error"] = "No bills found for this owner.";
                return RedirectToAction(nameof(View), new { ownerId });
            }

            // Compute total due
            var totalDue = allocs.Sum(a => a.AmountDue - a.Payments.Sum(p => p.Amount));
            if (totalDue <= 0)
            {
                TempData["Error"] = "Nothing due to pay.";
                return RedirectToAction(nameof(View), new { ownerId });
            }

            // Record payments for the exact due on each allocation
            foreach (var a in allocs.OrderBy(x => x.CommonBill!.BillDate).ThenBy(x => x.Id))
            {
                var due = a.AmountDue - a.Payments.Sum(p => p.Amount);
                if (due <= 0) continue;

                await _db.ExpenseAllocationPayments.AddAsync(new ExpenseAllocationPayment
                {
                    ExpenseAllocationId = a.Id,
                    Amount = due,
                    PaymentDate = DateTime.Today,
                    Reference = "PayAll",
                    CommonBillId = a.CommonBillId,
                    OwnerId = ownerId
                });

                a.IsPaid = true;
                a.PaymentDate = DateTime.Today;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"All outstanding dues paid ({totalDue:C}).";
            return RedirectToAction(nameof(View), new { ownerId });
        }

        // POST: /OwnerBilling/FullPay
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FullPay(string ownerId, Guid commonBillId)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || commonBillId == Guid.Empty)
                return BadRequest();

            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            // Fetch allocations for this owner on this specific bill
            var q = _db.ExpenseAllocations
                .Include(a => a.Payments)
                .Include(a => a.CommonBill)
                .Where(a => a.OwnerId == ownerId && a.CommonBillId == commonBillId);

            if (User.IsInRole("President"))
                q = q.Where(a => a.CommonBill!.BuildingId == me.BuildingId);

            var allocs = await q.ToListAsync();
            if (allocs.Count == 0)
            {
                TempData["Error"] = "No allocations found for this owner and bill.";
                return RedirectToAction(nameof(View), new { ownerId });
            }

            // Total due for this bill
            var totalDue = allocs.Sum(a => a.AmountDue - a.Payments.Sum(p => p.Amount));
            if (totalDue <= 0)
            {
                TempData["Error"] = "This bill has no due amount.";
                return RedirectToAction(nameof(View), new { ownerId });
            }

            var today = DateTime.Today;

            // Pay exact remaining due across all allocations for this bill
            foreach (var a in allocs.OrderBy(x => x.Id))
            {
                var already = a.Payments.Sum(p => p.Amount);
                var due = a.AmountDue - already;
                if (due <= 0) continue;

                await _db.ExpenseAllocationPayments.AddAsync(new ExpenseAllocationPayment
                {
                    ExpenseAllocationId = a.Id,
                    Amount = due,
                    PaymentDate = today,
                    Reference = "FullPay",
                    CommonBillId = a.CommonBillId,
                    OwnerId = ownerId
                });

                a.IsPaid = true;
                a.PaymentDate = today;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Bill fully paid.";
            return RedirectToAction(nameof(View), new { ownerId });
        }
    }
}
