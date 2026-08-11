using System.Globalization;
using System.Text;
using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Reports;
using ApartmentManagementSystem.Features.Reports.Services;
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
        private readonly IPresidentFinancialReportService _financialReports;
        private readonly IPresidentOccupancyReportService _occupancyReports;
        private readonly IPresidentVisitorReportService _visitorReports;

        public PresidentReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IPresidentFinancialReportService financialReports, IPresidentOccupancyReportService occupancyReports, IPresidentVisitorReportService visitorReports)
        {
            _context = context;
            _userManager = userManager;
            _financialReports = financialReports;
            _occupancyReports = occupancyReports;
            _visitorReports = visitorReports;
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
            return View(await _financialReports.GetAsync(buildingId, buildingName, filter));
        }

        public async Task<IActionResult> FinancialCsv(DateTime? from, DateTime? to)
        {
            var filter = new DateRangeFilter { From = from, To = to };
            var (buildingId, buildingName) = await RequireBuilding();
            var rows = await _financialReports.GetCsvAsync(buildingId, filter);

            var sb = new StringBuilder();
            sb.AppendLine($"Building,{Csv(buildingName)}");
            sb.AppendLine("BillDate,Title,TotalAmount,Collected,Outstanding");

            foreach (var row in rows)
            {
                var outstanding = Math.Max(row.TotalAmount - row.Collected, 0m);
                sb.AppendLine($"{row.BillDate:yyyy-MM-dd},{Csv(row.Title)},{row.TotalAmount},{row.Collected},{outstanding}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "financial_report.csv");
        }

        public async Task<IActionResult> Occupancy()
        {
            var (buildingId, buildingName) = await RequireBuilding();

            return View(await _occupancyReports.GetAsync(buildingId, buildingName));
        }

        public async Task<IActionResult> OccupancyCsv()
        {
            var (buildingId, buildingName) = await RequireBuilding();
            var flats = await _occupancyReports.GetCsvAsync(buildingId);

            var sb = new StringBuilder();
            sb.AppendLine($"Building,{Csv(buildingName)}");
            sb.AppendLine("FlatNumber,IsOccupied,HasOwner");
            foreach (var f in flats)
                sb.AppendLine($"{Csv(f.FlatNumber)}, {(f.IsOccupied ? "Yes" : "No")}, {(f.HasOwner ? "Yes" : "No")}");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "occupancy_report.csv");
        }

        public async Task<IActionResult> Visitors(DateRangeFilter filter)
        {
            var (buildingId, buildingName) = await RequireBuilding();
            return View(await _visitorReports.GetAsync(buildingId, buildingName, filter));
        }

        public async Task<IActionResult> VisitorsCsv(DateTime? from, DateTime? to)
        {
            var filter = new DateRangeFilter { From = from, To = to };
            var (buildingId, buildingName) = await RequireBuilding();
            var rows = await _visitorReports.GetCsvAsync(buildingId, filter);

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
