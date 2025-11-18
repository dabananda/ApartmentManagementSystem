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

        public PresidentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<PresidentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var claim = User.FindFirst("building_id")?.Value;
            if (!Guid.TryParse(claim, out var buildingId))
            {
                TempData["DashboardNotice"] = "Your account isn’t linked to a building yet. Please contact a Super Admin.";
                return View(new PresidentDashboardViewModel());
            }

            var buildingName = await _context.Buildings.AsNoTracking()
                .Where(b => b.Id == buildingId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync() ?? "(Building)";

            var totalBills = await _context.CommonBills.AsNoTracking()
                .CountAsync(b => b.BuildingId == buildingId);

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

            var totalFlats = await _context.Flats.AsNoTracking()
                .CountAsync(f => f.BuildingId == buildingId);

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
                .OrderByDescending(t => t.OccurredAt)
                .Take(25)
                .ToList();

            var firstOfThisMonthUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startMonthUtc = firstOfThisMonthUtc.AddMonths(-11);

            var labels = new List<string>(12);
            var inSeries = new List<decimal>(new decimal[12]);
            var outSeries = new List<decimal>(new decimal[12]);

            for (int i = 0; i < 12; i++)
            {
                var m = startMonthUtc.AddMonths(i);
                labels.Add($"{m:yyyy-MM}");
            }

            var ownerInByMonth = await (
                from p in _context.ExpenseAllocationPayments.AsNoTracking()
                join b in _context.CommonBills.AsNoTracking() on p.CommonBillId equals b.Id
                where b.BuildingId == buildingId && p.CreatedAtUtc >= startMonthUtc
                group p by new { p.CreatedAtUtc.Year, p.CreatedAtUtc.Month } into g
                select new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) }
            ).ToListAsync();

            foreach (var row in ownerInByMonth)
            {
                var idx = ((row.Year * 12) + (row.Month - 1)) - ((startMonthUtc.Year * 12) + (startMonthUtc.Month - 1));
                if (idx >= 0 && idx < 12) inSeries[idx] = row.Amount;
            }

            var outByMonth = await _context.ExpensePayments.AsNoTracking()
                .Where(p => p.BuildingId == buildingId && p.CreatedAtUtc >= startMonthUtc)
                .GroupBy(p => new { p.CreatedAtUtc.Year, p.CreatedAtUtc.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) })
                .ToListAsync();

            foreach (var row in outByMonth)
            {
                var idx = ((row.Year * 12) + (row.Month - 1)) - ((startMonthUtc.Year * 12) + (startMonthUtc.Month - 1));
                if (idx >= 0 && idx < 12) outSeries[idx] = row.Amount;
            }

            var allocations = await _context.ExpenseAllocations.AsNoTracking()
                .Include(a => a.CommonBill)
                .Where(a => a.CommonBill!.BuildingId == buildingId)
                .Select(a => new
                {
                    a.Id,
                    a.OwnerId,
                    a.AmountDue,
                    BillDate = a.CommonBill!.BillDate
                })
                .ToListAsync();

            var paidPerAlloc = await _context.ExpenseAllocationPayments.AsNoTracking()
                .Join(_context.CommonBills.AsNoTracking(),
                      p => p.CommonBillId, b => b.Id,
                      (p, b) => new { p.ExpenseAllocationId, p.Amount, b.BuildingId })
                .Where(x => x.BuildingId == buildingId)
                .GroupBy(x => x.ExpenseAllocationId)
                .Select(g => new { ExpenseAllocationId = g.Key, Paid = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.ExpenseAllocationId, x => x.Paid);

            decimal d0_30 = 0, d31_60 = 0, d61_90 = 0, d90plus = 0;
            var ownerDueMap = new Dictionary<string, decimal>(StringComparer.Ordinal);

            var todayLocal = DateTime.Today;

            foreach (var a in allocations)
            {
                var paid = paidPerAlloc.TryGetValue(a.Id, out var v) ? v : 0m;
                var outstanding = a.AmountDue - paid;
                if (outstanding <= 0m) continue;

                var days = (todayLocal - a.BillDate.Date).TotalDays;

                if (days <= 30) d0_30 += outstanding;
                else if (days <= 60) d31_60 += outstanding;
                else if (days <= 90) d61_90 += outstanding;
                else d90plus += outstanding;

                if (!ownerDueMap.TryGetValue(a.OwnerId, out var cur))
                    ownerDueMap[a.OwnerId] = outstanding;
                else
                    ownerDueMap[a.OwnerId] = cur + outstanding;
            }

            var topOwners = ownerDueMap
                .OrderByDescending(kv => kv.Value)
                .Take(7)
                .ToList();

            var ownerIds = topOwners.Select(kv => kv.Key).ToList();
            var ownerNames = await _context.Users.AsNoTracking()
                .Where(u => ownerIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = (u.Fullname ?? u.UserName)! })
                .ToListAsync();
            var ownerNameMap = ownerNames.ToDictionary(x => x.Id, x => x.Name);

            var topLabels = new List<string>();
            var topValues = new List<decimal>();
            foreach (var kv in topOwners)
            {
                topLabels.Add(ownerNameMap.TryGetValue(kv.Key, out var nm) ? nm : kv.Key);
                topValues.Add(kv.Value);
            }

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
                RecentTransactions = recentTransactions,
                Cashflow = new CashflowChartVM
                {
                    Labels = labels,
                    In = inSeries,
                    Out = outSeries
                },
                Aging = new AgingBucketsVM
                {
                    D0_30 = d0_30,
                    D31_60 = d31_60,
                    D61_90 = d61_90,
                    D90Plus = d90plus
                },
                TopOwners = new TopOwnersVM
                {
                    Labels = topLabels,
                    Values = topValues
                }
            };

            return View(vm);
        }
    }
}
