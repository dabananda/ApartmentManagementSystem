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

        public PresidentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /President/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();
            var buildingId = user.BuildingId.Value;

            // ---------- Financials ----------
            var totalBills = await _context.CommonBills
                .AsNoTracking()
                .Where(b => b.BuildingId == buildingId)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;

            // Building-level outgoing payments still tracked in ExpensePayments (e.g., vendor)
            var totalPayments = await _context.ExpensePayments
                .AsNoTracking()
                .Where(p => p.BuildingId == buildingId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // Collected from owners = sum of per-owner allocation payments for this building
            var totalCollected = await _context.ExpenseAllocationPayments
                .AsNoTracking()
                .Where(p => p.CommonBillId != Guid.Empty)
                .Join(_context.CommonBills.AsNoTracking().Where(b => b.BuildingId == buildingId),
                      p => p.CommonBillId, b => b.Id, (p, b) => p.Amount)
                .SumAsync(a => (decimal?)a) ?? 0m;

            // ---------- Occupancy ----------
            var totalFlats = await _context.Flats
                .AsNoTracking()
                .CountAsync(f => f.BuildingId == buildingId);

            var occupiedFlats = await _context.Flats
                .AsNoTracking()
                .CountAsync(f => f.BuildingId == buildingId && f.IsOccupied);

            // ---------- Entry logs ----------
            var todayStart = DateTime.Today;
            var tomorrowStart = todayStart.AddDays(1);
            var last7Start = todayStart.AddDays(-6);

            var entriesQuery = _context.EntryLogs
                .AsNoTracking()
                .Where(el => el.BuildingId == buildingId);

            var todayEntries = await entriesQuery
                .CountAsync(el => el.EntryTime >= todayStart && el.EntryTime < tomorrowStart);

            var last7dEntries = await entriesQuery
                .CountAsync(el => el.EntryTime >= last7Start && el.EntryTime < tomorrowStart);

            // Group by the enum itself; convert to string AFTER materialization
            var entryGroups = await entriesQuery
                .GroupBy(el => el.EntryType) // NOTE: if your property is EntryCategory, swap to el.EntryCategory.
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            var entryByCategory = entryGroups.ToDictionary(
                x => x.Category.ToString(),
                x => x.Count
            );

            // ---------- Announcements & Maintenance ----------
            var recentAnnouncements = await _context.Announcements
                .AsNoTracking()
                .Where(a => a.BuildingId == buildingId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.Title)
                .Take(5)
                .ToListAsync();

            var openTickets = await _context.MaintenanceTickets
                .AsNoTracking()
                .Where(t => t.BuildingId == buildingId && t.Status != "Closed")
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => $"{t.Title} ({t.Status})")
                .Take(5)
                .ToListAsync();

            // ---------- Building name ----------
            var buildingName = await _context.Buildings
                .AsNoTracking()
                .Where(b => b.Id == buildingId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync() ?? "My Building";

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
                EntryByCategory = entryByCategory,

                RecentAnnouncements = recentAnnouncements,
                OpenMaintenance = openTickets
            };

            return View(vm);
        }
    }
}
