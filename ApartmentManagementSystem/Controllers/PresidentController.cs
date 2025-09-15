using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.President;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class PresidentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PresidentController> _logger;

        public PresidentController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<PresidentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /President/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var claim = User.FindFirst("building_id")?.Value;
            if (!Guid.TryParse(claim, out var buildingId))
            {
                TempData["DashboardNotice"] = "Your account isn’t linked to a building yet. Please contact a Super Admin.";
                return View(new PresidentDashboardViewModel
                {
                    BuildingName = "(no building)",
                    TotalBills = 0,
                    TotalCollected = 0m,
                    TotalPayments = 0m,
                    TotalFlats = 0,
                    OccupiedFlats = 0,
                    TodayEntries = 0,
                    Last7dEntries = 0,
                    RecentTransactions = new List<TransactionRowViewModel>()
                });
            }

            // Header
            var buildingName = await _context.Buildings
                .AsNoTracking()
                .Where(b => b.Id == buildingId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync() ?? "(Building)";

            // === Totals ===
            var totalBills = await _context.CommonBills.AsNoTracking().CountAsync(b => b.BuildingId == buildingId);

            var totalCollected = await (
                from p in _context.ExpenseAllocationPayments.AsNoTracking()
                join b in _context.CommonBills.AsNoTracking() on p.CommonBillId equals b.Id
                where b.BuildingId == buildingId
                select (decimal?)p.Amount
            ).SumAsync() ?? 0m;

            var totalPayments = await _context.ExpensePayments.AsNoTracking()
                .Where(p => p.BuildingId == buildingId)
                .Select(p => (decimal?)p.Amount)
                .SumAsync() ?? 0m;

            var totalFlats = await _context.Flats.AsNoTracking().CountAsync(f => f.BuildingId == buildingId);
            var occupiedFlats = await _context.TenantAssignments.AsNoTracking()
                .Include(a => a.Flat)
                .Where(a => a.EndDate == null && a.Flat!.BuildingId == buildingId)
                .Select(a => a.FlatId)
                .Distinct()
                .CountAsync();

            var todayUtc = DateTime.UtcNow.Date;
            var last7dUtc = todayUtc.AddDays(-6);

            var todayEntries = await _context.EntryLogs.AsNoTracking()
                .Where(e => e.BuildingId == buildingId && e.EntryTime.Date == todayUtc)
                .CountAsync();

            var last7dEntries = await _context.EntryLogs.AsNoTracking()
                .Where(e => e.BuildingId == buildingId && e.EntryTime.Date >= last7dUtc)
                .CountAsync();

            // === Recent Activity (with precise timestamps & owner names) ===

            var recentOwnerPayments = await (
                from p in _context.ExpenseAllocationPayments.AsNoTracking()
                join b in _context.CommonBills.AsNoTracking() on p.CommonBillId equals b.Id
                join u in _context.Users.AsNoTracking() on p.OwnerId equals u.Id
                where b.BuildingId == buildingId
                orderby p.CreatedAtUtc descending
                select new TransactionRowViewModel
                {
                    OccurredAt = p.CreatedAtUtc,
                    Type = "OwnerPayment",
                    Description = $"Owner {(u.Fullname ?? u.UserName)} paid for bill “{b.Name}”.",
                    Amount = p.Amount,
                    Currency = "BDT",
                    Direction = "In"
                })
                .Take(60)
                .ToListAsync();

            var recentExpensePayments = await _context.ExpensePayments.AsNoTracking()
                .Where(p => p.BuildingId == buildingId)
                .Include(p => p.CommonBill)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Select(p => new TransactionRowViewModel
                {
                    OccurredAt = p.CreatedAtUtc,
                    Type = "ExpensePayment",
                    Description = p.CommonBill != null
                        ? $"Payment made for bill “{p.CommonBill.Name}”."
                        : "Expense payment recorded.",
                    Amount = p.Amount,
                    Currency = "BDT",
                    Direction = "Out"
                })
                .Take(60)
                .ToListAsync();

            var recentBills = await _context.CommonBills.AsNoTracking()
                .Where(b => b.BuildingId == buildingId)
                .OrderByDescending(b => b.CreatedAtUtc)
                .Select(b => new TransactionRowViewModel
                {
                    OccurredAt = b.CreatedAtUtc,
                    Type = "CommonBillCreated",
                    Description = $"Common bill created: “{b.Name}”.",
                    Amount = b.TotalAmount,
                    Currency = "BDT",
                    Direction = "Info"
                })
                .Take(60)
                .ToListAsync();

            var recentEntryLogs = await _context.EntryLogs.AsNoTracking()
                .Where(el => el.BuildingId == buildingId)
                .OrderByDescending(el => el.EntryTime)
                .Select(el => new TransactionRowViewModel
                {
                    // Normalize to UTC for consistent client-side rendering
                    OccurredAt = DateTime.SpecifyKind(el.EntryTime, DateTimeKind.Local).ToUniversalTime(),
                    Type = "EntryLog",
                    Description = $"{el.EntryType} — {el.Fullname}",
                    Amount = null,
                    Currency = null,
                    Direction = "Info"
                })
                .Take(60)
                .ToListAsync();

            var recentTransactions = recentOwnerPayments
                .Concat(recentExpensePayments)
                .Concat(recentBills)
                .Concat(recentEntryLogs)
                .OrderByDescending(t => t.OccurredAt) // precise to the second now
                .Take(25)
                .ToList();

            var vm = new PresidentDashboardViewModel
            {
                BuildingName = buildingName,
                TotalBills = totalBills,
                TotalCollected = totalCollected,
                TotalPayments = totalPayments,
                TotalFlats = totalFlats,
                OccupiedFlats = occupiedFlats,
                TodayEntries = todayEntries,
                Last7dEntries = last7dEntries,
                RecentTransactions = recentTransactions
            };

            return View(vm);
        }
    }
}
