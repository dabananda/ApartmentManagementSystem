using System.Globalization;
using System.Text;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Features.Buildings.Repositories;
using ApartmentManagementSystem.Features.Reports.Services;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Reports.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Features.Reports
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class PresidentReportsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBuildingRepository _buildings;
        private readonly IPresidentFinancialReportService _financialReports;
        private readonly IPresidentOccupancyReportService _occupancyReports;
        private readonly IPresidentVisitorReportService _visitorReports;
        private readonly IMaintenanceReportService _maintenanceReports;

        public PresidentReportsController(
            UserManager<ApplicationUser> userManager,
            IBuildingRepository buildings,
            IPresidentFinancialReportService financialReports,
            IPresidentOccupancyReportService occupancyReports,
            IPresidentVisitorReportService visitorReports,
            IMaintenanceReportService maintenanceReports)
        {
            _userManager = userManager;
            _buildings = buildings;
            _financialReports = financialReports;
            _occupancyReports = occupancyReports;
            _visitorReports = visitorReports;
            _maintenanceReports = maintenanceReports;
        }

        private async Task<(Guid buildingId, string buildingName)> RequireBuilding()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId == null)
                throw new InvalidOperationException("User has no building.");
            var bId = me.BuildingId.Value;
            var building = await _buildings.GetAsync(bId);
            var bName = building?.Name ?? "My Building";
            return (bId, bName);
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
            return View(await _maintenanceReports.GetAsync(buildingId, buildingName, filter));
        }

        public async Task<IActionResult> MaintenanceCsv(DateTime? from, DateTime? to)
        {
            var filter = new DateRangeFilter { From = from, To = to };
            var (buildingId, buildingName) = await RequireBuilding();
            var rows = await _maintenanceReports.GetCsvAsync(buildingId, filter);

            var sb = new StringBuilder();
            sb.AppendLine($"Building,{Csv(buildingName)}");
            sb.AppendLine("Title,Status,CreatedAt,ClosedAt,ResolutionHours");

            foreach (var r in rows)
            {
                var title = Csv(r.Title);
                var created = r.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                var closed = r.ClosedAt.HasValue ? r.ClosedAt.Value.ToString("yyyy-MM-dd HH:mm") : "";
                var hours = r.ClosedAt.HasValue
                    ? (r.ClosedAt.Value - r.CreatedAt).TotalHours.ToString("0.##", CultureInfo.InvariantCulture)
                    : "";
                sb.AppendLine($"{title},{r.Status},{created},{closed},{hours}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "maintenance_report.csv");
        }
    }
}
