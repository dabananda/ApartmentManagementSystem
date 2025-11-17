using System.Globalization;
using System.Text;
using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class PresidentReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PresidentReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<(Guid buildingId, string buildingName)> RequireBuilding()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId == null)
                throw new InvalidOperationException("User has no building.");
            var bId = me.BuildingId.Value;
            var bName = await _context.Buildings.AsNoTracking()
                .Where(b => b.Id == bId).Select(b => b.Name).FirstOrDefaultAsync() ?? "My Building";
            return (bId, bName);
        }

        private static string BillTitle(object bill)
        {
            return ReadStringProp(bill, "Title", "Name", "Description", "BillName", "Purpose")
                   ?? $"Bill {ReadStringProp(bill, "Id") ?? ""}";
        }

        private static string? ReadStringProp(object obj, params string[] names)
        {
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n);
                if (p == null) continue;
                var v = p.GetValue(obj);
                if (v == null) continue;
                return v.ToString();
            }
            return null;
        }

        private static string Csv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        public async Task<IActionResult> Financial(DateRangeFilter filter)
        {
            var (buildingId, buildingName) = await RequireBuilding();
            var (start, endExclusive) = filter.ToBoundsOrDefault(60);

            // Pull bills as entities (so we can compute a title via reflection)
            var bills = await _context.CommonBills.AsNoTracking()
                .Where(b => b.BuildingId == buildingId && b.BillDate >= start && b.BillDate < endExclusive)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

            var billIds = bills.Select(b => b.Id).ToList();

            var collectedByBill = await _context.ExpenseAllocationPayments.AsNoTracking()
                .Where(p => billIds.Contains(p.CommonBillId) && p.Status == PaymentStatus.Succeeded)
                .GroupBy(p => p.CommonBillId)
                .Select(g => new { BillId = g.Key, Collected = g.Sum(x => x.Amount) })
                .ToListAsync();
            var collectedLookup = collectedByBill.ToDictionary(x => x.BillId, x => x.Collected);

            var paymentsTotal = await _context.ExpensePayments.AsNoTracking()
                .Where(p => p.BuildingId == buildingId && p.PaymentDate >= start && p.PaymentDate < endExclusive)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var rows = bills
                .Select(b => new FinancialReportRow
                {
                    CommonBillId = b.Id,
                    Title = BillTitle(b),
                    BillDate = b.BillDate,
                    TotalAmount = b.TotalAmount,
                    Collected = collectedLookup.TryGetValue(b.Id, out var c) ? c : 0m,
                    Payments = 0m // per-bill payments not tracked; totals shown below
                })
                .ToList();

            var vm = new FinancialReportViewModel
            {
                BuildingName = buildingName,
                Filter = filter,
                TotalBills = rows.Sum(r => r.TotalAmount),
                TotalCollected = rows.Sum(r => r.Collected),
                TotalPayments = paymentsTotal,
                Rows = rows
            };

            return View(vm);
        }

        public async Task<IActionResult> FinancialCsv(DateTime? from, DateTime? to)
        {
            var filter = new DateRangeFilter { From = from, To = to };
            var (buildingId, buildingName) = await RequireBuilding();
            var (start, endExclusive) = filter.ToBoundsOrDefault(60);

            var bills = await _context.CommonBills.AsNoTracking()
                .Where(b => b.BuildingId == buildingId && b.BillDate >= start && b.BillDate < endExclusive)
                .OrderBy(b => b.BillDate)
                .ToListAsync();

            var billIds = bills.Select(b => b.Id).ToList();

            var collectedByBill = await _context.ExpenseAllocations.AsNoTracking()
                .Where(a => billIds.Contains(a.CommonBillId) && a.IsPaid)
                .GroupBy(a => a.CommonBillId)
                .Select(g => new { BillId = g.Key, Collected = g.Sum(x => x.AmountDue) })
                .ToListAsync();
            var collectedLookup = collectedByBill.ToDictionary(x => x.BillId, x => x.Collected);

            var sb = new StringBuilder();
            sb.AppendLine($"Building,{Csv(buildingName)}");
            sb.AppendLine("BillDate,Title,TotalAmount,Collected,Outstanding");

            foreach (var b in bills)
            {
                var collected = collectedLookup.TryGetValue(b.Id, out var c) ? c : 0m;
                var outstanding = Math.Max(b.TotalAmount - collected, 0m);
                sb.AppendLine($"{b.BillDate:yyyy-MM-dd},{Csv(BillTitle(b))},{b.TotalAmount},{collected},{outstanding}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "financial_report.csv");
        }

        public async Task<IActionResult> Occupancy()
        {
            var (buildingId, buildingName) = await RequireBuilding();

            var totalFlats = await _context.Flats.AsNoTracking().CountAsync(f => f.BuildingId == buildingId);
            var occupiedFlats = await _context.Flats.AsNoTracking().CountAsync(f => f.BuildingId == buildingId && f.IsOccupied);

            var ownersCount = await _context.Flats.AsNoTracking()
                .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
                .Select(f => f.OwnerId)
                .Distinct()
                .CountAsync();

            // Count active tenant assignments for flats in this building (handles new assignment-based model)
            var tenantsCount = await (from ta in _context.TenantAssignments.AsNoTracking()
                                      join f in _context.Flats.AsNoTracking() on ta.FlatId equals f.Id
                                      where f.BuildingId == buildingId && (ta.EndDate == null || ta.EndDate >= DateTime.Today)
                                      select ta)
                                     .CountAsync();

            var vm = new OccupancyReportViewModel
            {
                BuildingName = buildingName,
                TotalFlats = totalFlats,
                OccupiedFlats = occupiedFlats,
                OwnersCount = ownersCount,
                TenantsCount = tenantsCount
            };
            return View(vm);
        }

        public async Task<IActionResult> OccupancyCsv()
        {
            var (buildingId, buildingName) = await RequireBuilding();
            var flats = await _context.Flats.AsNoTracking()
                .Where(f => f.BuildingId == buildingId)
                .Select(f => new { f.FlatNumber, f.IsOccupied, f.OwnerId })
                .OrderBy(f => f.FlatNumber)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Building,{Csv(buildingName)}");
            sb.AppendLine("FlatNumber,IsOccupied,HasOwner");
            foreach (var f in flats)
                sb.AppendLine($"{Csv(f.FlatNumber)}, {(f.IsOccupied ? "Yes" : "No")}, {(f.OwnerId != null ? "Yes" : "No")}");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "occupancy_report.csv");
        }

        public async Task<IActionResult> Visitors(DateRangeFilter filter)
        {
            var (buildingId, buildingName) = await RequireBuilding();
            var (start, endExclusive) = filter.ToBoundsOrDefault(30);

            var query = _context.EntryLogs.AsNoTracking()
                .Where(e => e.BuildingId == buildingId && e.EntryTime >= start && e.EntryTime < endExclusive);

            // Group by enum server-side, stringify after materialization
            var cats = await query
                .GroupBy(e => e.EntryType) // If your property is EntryCategory, change here & in CSV
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var byCategory = cats.ToDictionary(x => x.Type.ToString(), x => x.Count);

            var inRange = await query.Select(e => new { e.EntryTime }).ToListAsync();
            var daily = inRange
                .GroupBy(x => x.EntryTime.Date)
                .OrderBy(g => g.Key)
                .Select(g => (Day: g.Key, Count: g.Count()))
                .ToList();

            var vm = new VisitorReportViewModel
            {
                BuildingName = buildingName,
                Filter = filter,
                TotalEntries = inRange.Count,
                ByCategory = byCategory,
                DailyCounts = daily
            };
            return View(vm);
        }

        public async Task<IActionResult> VisitorsCsv(DateTime? from, DateTime? to)
        {
            var filter = new DateRangeFilter { From = from, To = to };
            var (buildingId, buildingName) = await RequireBuilding();
            var (start, endExclusive) = filter.ToBoundsOrDefault(30);

            var rows = await _context.EntryLogs.AsNoTracking()
                .Where(e => e.BuildingId == buildingId && e.EntryTime >= start && e.EntryTime < endExclusive)
                .Select(e => new { e.EntryTime, e.EntryType, e.Fullname, e.FlatId })
                .OrderBy(e => e.EntryTime)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Building,{Csv(buildingName)}");
            sb.AppendLine("DateTime,Type,Person,FlatId");
            foreach (var r in rows)
                sb.AppendLine($"{r.EntryTime:yyyy-MM-dd HH:mm},{Csv(r.EntryType.ToString())},{Csv(r.Fullname)},{r.FlatId}");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "visitor_report.csv");
        }

        public async Task<IActionResult> Maintenance(DateRangeFilter filter)
        {
            var (buildingId, buildingName) = await RequireBuilding();
            var (start, endExclusive) = filter.ToBoundsOrDefault(90);

            var q = _context.MaintenanceTickets.AsNoTracking().Where(t => t.BuildingId == buildingId);

            var open = await q.CountAsync(t => t.Status == "Open");
            var inProgress = await q.CountAsync(t => t.Status == "InProgress");
            var closed = await q.CountAsync(t => t.Status == "Closed");

            var createdInRange = await q.CountAsync(t => t.CreatedAt >= start && t.CreatedAt < endExclusive);
            var closedInRange = await q.CountAsync(t => t.ClosedAt != null && t.ClosedAt >= start && t.ClosedAt < endExclusive);

            var resolved = await q.Where(t => t.Status == "Closed" && t.ClosedAt != null)
                .Select(t => new { t.CreatedAt, t.ClosedAt })
                .ToListAsync();

            double? avgHours = null;
            if (resolved.Count > 0)
            {
                avgHours = resolved.Average(r => (r.ClosedAt!.Value - r.CreatedAt).TotalHours);
            }

            var vm = new MaintenanceReportViewModel
            {
                BuildingName = buildingName,
                Filter = filter,
                OpenCount = open,
                InProgressCount = inProgress,
                ClosedCount = closed,
                AvgResolutionHours = avgHours,
                NewlyCreated = createdInRange,
                ClosedInRange = closedInRange
            };
            return View(vm);
        }

        public async Task<IActionResult> MaintenanceCsv(DateTime? from, DateTime? to)
        {
            var filter = new DateRangeFilter { From = from, To = to };
            var (buildingId, buildingName) = await RequireBuilding();
            var (start, endExclusive) = filter.ToBoundsOrDefault(90);

            var rows = await _context.MaintenanceTickets.AsNoTracking()
                .Where(t => t.BuildingId == buildingId && t.CreatedAt >= start && t.CreatedAt < endExclusive)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Building,{Csv(buildingName)}");
            sb.AppendLine("Title,Status,CreatedAt,ClosedAt,ResolutionHours");

            foreach (var r in rows)
            {
                var title = Csv(r.Title);
                var created = r.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                var closed = r.ClosedAt.HasValue ? r.ClosedAt.Value.ToString("yyyy-MM-dd HH:mm") : "";
                var hours = r.ClosedAt.HasValue ? (r.ClosedAt.Value - r.CreatedAt).TotalHours.ToString("0.##", CultureInfo.InvariantCulture) : "";
                sb.AppendLine($"{title},{r.Status},{created},{closed},{hours}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "maintenance_report.csv");
        }
    }
}