using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.SuperAdmin)]
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
            var totalBuildings = await _context.Buildings.CountAsync();
            var buildings = await _context.Buildings
                .Include(b => b.Flats)
                .Include(b => b.CommonBills)
                .Include(b => b.ExpensePayments)
                .ToListAsync();

            var allUsers = await _userManager.Users.ToListAsync();
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var presidents = await _userManager.GetUsersInRoleAsync("President");
            var owners = await _userManager.GetUsersInRoleAsync("Owner");
            var tenants = await _userManager.GetUsersInRoleAsync("Tenant");
            var staffs = await _userManager.GetUsersInRoleAsync("Staff");
            var pendingUsers = await _userManager.GetUsersInRoleAsync("User");

            var occupiedFlatIds = new HashSet<Guid>(await _context.TenantAssignments
                .Where(ta => ta.EndDate == null)
                .Select(ta => ta.FlatId)
                .ToListAsync());

            var totalFlats = await _context.Flats.CountAsync();
            var occupiedFlatsCount = occupiedFlatIds.Count;
            var flatsWithOwners = await _context.Flats.CountAsync(f => f.OwnerId != null);

            var totalBillsAmount = await _context.CommonBills.SumAsync(b => b.TotalAmount);
            var totalPaymentsAmount = await _context.ExpensePayments.SumAsync(p => p.Amount);

            var totalCollectedAmount = await _context.ExpenseAllocationPayments
                .SumAsync(p => p.Amount);

            var totalAllocatedAmount = await _context.ExpenseAllocations
                .SumAsync(a => a.AmountDue);

            var totalPendingAmount = totalAllocatedAmount - totalCollectedAmount;

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

            var buildingsSummary = buildings.Select(b =>
            {
                var bOccupied = b.Flats?.Count(f => occupiedFlatIds.Contains(f.Id)) ?? 0;
                var bTotalFlats = b.Flats?.Count ?? 0;

                return new BuildingSummaryViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    TotalFlats = bTotalFlats,
                    OccupiedFlats = bOccupied,
                    TotalBills = b.CommonBills?.Sum(cb => cb.TotalAmount) ?? 0,
                    TotalPayments = b.ExpensePayments?.Sum(ep => ep.Amount) ?? 0,
                    Balance = (b.CommonBills?.Sum(cb => cb.TotalAmount) ?? 0) - (b.ExpensePayments?.Sum(ep => ep.Amount) ?? 0)
                };
            }).ToList();

            var viewModel = new SuperAdminDashboardViewModel
            {
                TotalBuildings = totalBuildings,
                BuildingsSummary = buildingsSummary,

                TotalUsers = allUsers.Count,
                TotalSuperAdmins = superAdmins.Count,
                TotalPresidents = presidents.Count,
                TotalOwners = owners.Count,
                TotalStaffs = staffs.Count,
                PendingApprovals = pendingUsers.Count,
                TotalTenants = tenants.Count,

                TotalFlats = totalFlats,
                OccupiedFlats = occupiedFlatsCount,
                VacantFlats = totalFlats - occupiedFlatsCount,
                FlatsWithOwners = flatsWithOwners,
                FlatsWithoutOwners = totalFlats - flatsWithOwners,

                TotalBillsGenerated = totalBillsAmount,
                TotalPaymentsMade = totalPaymentsAmount,
                TotalAmountCollected = totalCollectedAmount,
                TotalPendingCollection = totalPendingAmount,
                OverallBalance = totalCollectedAmount - totalPaymentsAmount,

                RecentBills = recentBills,
                RecentPayments = recentPayments
            };

            return View(viewModel);
        }
    }
}