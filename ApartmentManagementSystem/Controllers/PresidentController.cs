using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            var totalPayments = await _context.ExpensePayments
                .AsNoTracking()
                .Where(p => p.BuildingId == buildingId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var totalCollected = await _context.ExpenseAllocations
                .AsNoTracking()
                .Include(a => a.CommonBill)
                .Where(a => a.CommonBill.BuildingId == buildingId && a.IsPaid)
                .SumAsync(a => (decimal?)a.AmountDue) ?? 0m;

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

                // placeholders until real models exist
                RecentAnnouncements = new List<string>(),
                OpenMaintenance = new List<string>()
            };

            return View(vm);
        }
    }
}
