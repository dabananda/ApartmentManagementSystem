using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SuperAdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Buildings Overview
            var totalBuildings = await _context.Buildings.CountAsync();
            var buildings = await _context.Buildings
                .Include(b => b.Flats)
                .Include(b => b.CommonBills)
                .Include(b => b.ExpensePayments)
                .ToListAsync();

            // Users Overview
            var allUsers = await _userManager.Users.ToListAsync();
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var presidents = await _userManager.GetUsersInRoleAsync("President");
            var owners = await _userManager.GetUsersInRoleAsync("Owner");
            var pendingUsers = await _userManager.GetUsersInRoleAsync("User"); // Users awaiting approval
            var totalTenants = await _context.Tenants.CountAsync();

            // Flats Overview
            var totalFlats = await _context.Flats.CountAsync();
            var occupiedFlats = await _context.Flats.CountAsync(f => f.IsOccupied);
            var flatsWithOwners = await _context.Flats.CountAsync(f => f.OwnerId != null);

            // Financial Overview
            var totalBillsAmount = await _context.CommonBills.SumAsync(b => b.TotalAmount);
            var totalPaymentsAmount = await _context.ExpensePayments.SumAsync(p => p.Amount);
            var totalCollectedAmount = await _context.ExpenseAllocations
                .Where(a => a.IsPaid)
                .SumAsync(a => a.AmountDue);
            var totalPendingAmount = await _context.ExpenseAllocations
                .Where(a => !a.IsPaid)
                .SumAsync(a => a.AmountDue);

            // Recent Activities
            var recentBills = await _context.CommonBills
                .Include(b => b.Building)
                .OrderByDescending(b => b.BillDate)
                .Take(5)
                .ToListAsync();

            var recentPayments = await _context.ExpensePayments
                .Include(p => p.Building)
                .Include(p => p.CommonBill)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            // Buildings with Financial Summary
            var buildingsSummary = buildings.Select(b => new BuildingSummaryViewModel
            {
                Id = b.Id,
                Name = b.Name,
                Address = b.Address,
                TotalFlats = b.Flats?.Count ?? 0,
                OccupiedFlats = b.Flats?.Count(f => f.IsOccupied) ?? 0,
                TotalBills = b.CommonBills?.Sum(cb => cb.TotalAmount) ?? 0,
                TotalPayments = b.ExpensePayments?.Sum(ep => ep.Amount) ?? 0,
                Balance = (b.CommonBills?.Sum(cb => cb.TotalAmount) ?? 0) - (b.ExpensePayments?.Sum(ep => ep.Amount) ?? 0)
            }).ToList();

            var viewModel = new SuperAdminDashboardViewModel
            {
                // Buildings Data
                TotalBuildings = totalBuildings,
                BuildingsSummary = buildingsSummary,

                // Users Data
                TotalUsers = allUsers.Count,
                TotalSuperAdmins = superAdmins.Count,
                TotalPresidents = presidents.Count,
                TotalOwners = owners.Count,
                PendingApprovals = pendingUsers.Count,
                TotalTenants = totalTenants,

                // Flats Data
                TotalFlats = totalFlats,
                OccupiedFlats = occupiedFlats,
                VacantFlats = totalFlats - occupiedFlats,
                FlatsWithOwners = flatsWithOwners,
                FlatsWithoutOwners = totalFlats - flatsWithOwners,

                // Financial Data
                TotalBillsGenerated = totalBillsAmount,
                TotalPaymentsMade = totalPaymentsAmount,
                TotalAmountCollected = totalCollectedAmount,
                TotalPendingCollection = totalPendingAmount,
                OverallBalance = totalCollectedAmount - totalPaymentsAmount,

                // Recent Activities
                RecentBills = recentBills,
                RecentPayments = recentPayments
            };

            return View(viewModel);
        }
    }
}