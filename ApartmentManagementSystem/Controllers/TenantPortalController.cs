using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.TenantPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.Tenant)]
    public class TenantPortalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public TenantPortalController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        public async Task<IActionResult> Dashboard()
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            var today = DateTime.Today;
            var assignment = await _db.TenantAssignments
                .Include(a => a.Flat)!.ThenInclude(f => f.Building)
                .Where(a => a.TenantUserId == me.Id && (a.EndDate == null || a.EndDate >= today))
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync();

            if (assignment?.Flat == null)
            {
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");
            }

            var flatId = assignment.FlatId;
            var buildingId = assignment.Flat.BuildingId;

            var bills = await _db.TenantBills
                .Include(b => b.Payments)
                .Where(b => b.TenantUserId == me.Id)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

            var total = bills.Sum(b => b.Amount);
            var paid = bills.Sum(b => b.Payments.Sum(p => p.Amount));
            var due = total - paid;

            var monthStart = new DateTime(today.Year, today.Month, 1);
            var paidThisMonth = await _db.TenantPayments
                .Include(p => p.TenantBill)!.ThenInclude(b => b.Flat)
                .Where(p => p.PaymentDate >= monthStart && p.TenantBill!.TenantUserId == me.Id)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var recentBills = bills.Take(6).Select(b => new TenantBillRow
            {
                BillId = b.Id,
                Title = b.Title,
                BillDate = b.BillDate,
                Amount = b.Amount,
                Paid = b.Payments.Sum(p => p.Amount)
            }).ToList();

            var recentPayments = await _db.TenantPayments
                .Include(p => p.TenantBill)
                .Where(p => p.TenantBill!.TenantUserId == me.Id)
                .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
                .Take(6)
                .Select(p => new TenantPaymentRow
                {
                    PaymentId = p.Id,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Reference = p.Reference,
                    BillTitle = p.TenantBill!.Title,
                    BillDate = p.TenantBill!.BillDate
                })
                .ToListAsync();

            var notices = await _db.Announcements
                .AsNoTracking()
                .Where(a => a.BuildingId == buildingId || a.BuildingId == null)
                .OrderByDescending(a => a.CreatedAt)
                .Take(6)
                .ToListAsync();

            var vm = new TenantDashboardVM
            {
                TenantName = me.Fullname ?? me.UserName ?? "Me",
                BuildingName = assignment.Flat.Building!.Name,
                FlatNumber = assignment.Flat.FlatNumber,

                TotalBilled = total,
                TotalPaid = paid,
                PaidThisMonth = paidThisMonth,

                RecentBills = recentBills,
                RecentPayments = recentPayments,
                RecentNotices = notices
            };

            return View(vm);
        }

        public async Task<IActionResult> Bills()
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            var items = await _db.TenantBills
                .Include(b => b.Payments)
                .Include(b => b.Flat)!.ThenInclude(f => f.Building)
                .Where(b => b.TenantUserId == me.Id)
                .OrderByDescending(b => b.BillDate)
                .Select(b => new TenantBillRow
                {
                    BillId = b.Id,
                    Title = b.Title,
                    BillDate = b.BillDate,
                    Amount = b.Amount,
                    Paid = b.Payments.Sum(p => p.Amount),
                    BuildingName = b.Flat!.Building!.Name,
                    FlatNumber = b.Flat!.FlatNumber
                })
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Payments()
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            var items = await _db.TenantPayments
                .Include(p => p.TenantBill)!.ThenInclude(b => b.Flat)!.ThenInclude(f => f.Building)
                .Where(p => p.TenantBill!.TenantUserId == me.Id)
                .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
                .Select(p => new TenantPaymentRow
                {
                    PaymentId = p.Id,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Reference = p.Reference,
                    BillTitle = p.TenantBill!.Title,
                    BillDate = p.TenantBill!.BillDate,
                    BuildingName = p.TenantBill!.Flat!.Building!.Name,
                    FlatNumber = p.TenantBill!.Flat!.FlatNumber
                })
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Notices()
        {
            var me = await _users.GetUserAsync(User);
            if (me?.BuildingId == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a building yet.");

            var notices = await _db.Announcements
                .AsNoTracking()
                .Where(a => a.BuildingId == me.BuildingId || a.BuildingId == null)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(notices);
        }

        public async Task<IActionResult> Tickets()
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            var assignment = await _db.TenantAssignments
                .Include(a => a.Flat)
                .Where(a => a.TenantUserId == me.Id)
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync();

            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");

            var myUserId = me.Id;
            var myFlatId = assignment.FlatId;
            var buildingId = assignment.Flat.BuildingId;

            var items = await _db.MaintenanceTickets
                .AsNoTracking()
                .Where(t =>
                    t.BuildingId == buildingId &&
                    (t.CreatedByUserId == myUserId || (t.CreatedByUserId == null && t.FlatId == myFlatId)))
                .OrderBy(t => t.Status).ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        public IActionResult NewTicket() => View(new MaintenanceTicket());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewTicket(MaintenanceTicket model)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            var assignment = await _db.TenantAssignments
                .Include(a => a.Flat)
                .Where(a => a.TenantUserId == me.Id)
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync();

            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");

            if (!ModelState.IsValid) return View(model);

            model.Id = Guid.NewGuid();
            model.BuildingId = assignment.Flat.BuildingId;
            model.FlatId = assignment.FlatId;
            model.CreatedByUserId = me.Id;
            model.Status = "Open";
            model.CreatedAt = DateTime.UtcNow;

            _db.MaintenanceTickets.Add(model);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Ticket created successfully.";
            return RedirectToAction(nameof(Tickets));
        }

        public async Task<IActionResult> Visitors(DateTime? from = null, DateTime? to = null)
        {
            var me = await _users.GetUserAsync(User);
            if (me == null) return Forbid();

            var assignment = await _db.TenantAssignments
                .Include(a => a.Flat)
                .Where(a => a.TenantUserId == me.Id)
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync();

            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");

            var flatId = assignment.FlatId;
            var buildingId = assignment.Flat.BuildingId;

            var q = _db.EntryLogs.AsNoTracking()
                .Where(el => el.FlatId == flatId && el.BuildingId == buildingId);

            if (from.HasValue) q = q.Where(el => el.EntryTime >= from.Value);
            if (to.HasValue) q = q.Where(el => el.EntryTime <= to.Value);

            var items = await q.OrderByDescending(el => el.EntryTime).ToListAsync();
            return View(items);
        }
    }
}
