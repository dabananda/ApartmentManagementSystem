using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
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
            // Resolve building from claim
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

            // === Totals (SEQUENTIAL AWAITS — no DbContext concurrency) ===

            var totalBills = await _context.CommonBills
                .AsNoTracking()
                .Where(b => b.BuildingId == buildingId)
                .CountAsync();

            // sum nullable to avoid DefaultIfEmpty join issue
            var totalCollectedNullable = await (
                from p in _context.ExpenseAllocationPayments.AsNoTracking()
                join b in _context.CommonBills.AsNoTracking() on p.CommonBillId equals b.Id
                where b.BuildingId == buildingId
                select (decimal?)p.Amount
            ).SumAsync();
            var totalCollected = totalCollectedNullable ?? 0m;

            var totalPaymentsNullable = await _context.ExpensePayments
                .AsNoTracking()
                .Where(p => p.BuildingId == buildingId)
                .Select(p => (decimal?)p.Amount)
                .SumAsync();
            var totalPayments = totalPaymentsNullable ?? 0m;

            var totalFlats = await _context.Flats
                .AsNoTracking()
                .Where(f => f.BuildingId == buildingId)
                .CountAsync();

            // TenantAssignments has no BuildingId: scope via Flat.BuildingId
            var occupiedFlats = await _context.TenantAssignments
                .AsNoTracking()
                .Include(a => a.Flat)
                .Where(a => a.EndDate == null && a.Flat!.BuildingId == buildingId)
                .Select(a => a.FlatId)
                .Distinct()
                .CountAsync();

            var todayDate = DateTime.UtcNow.Date;
            var todayEntries = await _context.EntryLogs
                .AsNoTracking()
                .Where(e => e.BuildingId == buildingId && e.EntryTime.Date == todayDate)
                .CountAsync();

            var last7dDate = DateTime.UtcNow.Date.AddDays(-6);
            var last7dEntries = await _context.EntryLogs
                .AsNoTracking()
                .Where(e => e.BuildingId == buildingId && e.EntryTime.Date >= last7dDate)
                .CountAsync();

            // === Recent Transactions (SEQUENTIAL) ===

            var recentOwnerPayments = await _context.ExpenseAllocationPayments
                .AsNoTracking()
                .Join(_context.CommonBills.AsNoTracking().Where(b => b.BuildingId == buildingId),
                      p => p.CommonBillId, b => b.Id, (p, b) => new { p, b })
                .OrderByDescending(x => x.p.PaymentDate)
                .Select(x => new TransactionRowViewModel
                {
                    OccurredAt = x.p.PaymentDate,
                    Type = "OwnerPayment",
                    Description = $"Owner payment for bill “{x.b.Name}”.",
                    Amount = x.p.Amount,
                    Currency = "BDT",
                    Direction = "In"
                })
                .Take(60)
                .ToListAsync();

            var recentExpensePayments = await _context.ExpensePayments
                .AsNoTracking()
                .Where(p => p.BuildingId == buildingId)
                .Include(p => p.CommonBill)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new TransactionRowViewModel
                {
                    OccurredAt = p.PaymentDate,
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

            var recentBills = await _context.CommonBills
                .AsNoTracking()
                .Where(b => b.BuildingId == buildingId)
                .OrderByDescending(b => b.BillDate)
                .Select(b => new TransactionRowViewModel
                {
                    OccurredAt = b.BillDate,
                    Type = "CommonBillCreated",
                    Description = $"Common bill created: “{b.Name}”.",
                    Amount = b.TotalAmount,
                    Currency = "BDT",
                    Direction = "Info"
                })
                .Take(60)
                .ToListAsync();

            var recentEntryLogs = await _context.EntryLogs
                .AsNoTracking()
                .Where(el => el.BuildingId == buildingId)
                .OrderByDescending(el => el.EntryTime)
                .Select(el => new TransactionRowViewModel
                {
                    OccurredAt = el.EntryTime,
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
                .OrderByDescending(t => t.OccurredAt)
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
