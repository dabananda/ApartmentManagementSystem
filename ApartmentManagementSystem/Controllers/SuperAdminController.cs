using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
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

        // GET: SuperAdmin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // --- 1. Buildings Overview ---
            var totalBuildings = await _context.Buildings.CountAsync();
            var buildings = await _context.Buildings
                .Include(b => b.Flats)
                .Include(b => b.CommonBills)
                .Include(b => b.ExpensePayments)
                .ToListAsync();

            // --- 2. Users Overview ---
            var allUsers = await _userManager.Users.ToListAsync();
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var presidents = await _userManager.GetUsersInRoleAsync("President");
            var owners = await _userManager.GetUsersInRoleAsync("Owner");
            var tenants = await _userManager.GetUsersInRoleAsync("Tenant");
            var staffs = await _userManager.GetUsersInRoleAsync("Staff");
            var pendingUsers = await _userManager.GetUsersInRoleAsync("User"); // Users awaiting approval

            // --- 3. Occupancy Fix (Use TenantAssignments) ---
            // Fetch IDs of flats that have an active assignment (EndDate is null)
            var occupiedFlatIds = new HashSet<Guid>(await _context.TenantAssignments
                .Where(ta => ta.EndDate == null)
                .Select(ta => ta.FlatId)
                .ToListAsync());

            var totalFlats = await _context.Flats.CountAsync();
            // Calculate occupied based on active assignments, not the flag
            var occupiedFlatsCount = occupiedFlatIds.Count;
            var flatsWithOwners = await _context.Flats.CountAsync(f => f.OwnerId != null);

            // --- 4. Financial Overview Fix ---
            var totalBillsAmount = await _context.CommonBills.SumAsync(b => b.TotalAmount); // Total Invoiced
            var totalPaymentsAmount = await _context.ExpensePayments.SumAsync(p => p.Amount); // Total Spent by Building

            // Fix: Calculate actual collected amount from Payments table
            var totalCollectedAmount = await _context.ExpenseAllocationPayments
                .SumAsync(p => p.Amount);

            // Fix: Calculate total allocated (Due) from Allocations
            var totalAllocatedAmount = await _context.ExpenseAllocations
                .SumAsync(a => a.AmountDue);

            // Fix: Pending is Total Allocated - Total Collected
            var totalPendingAmount = totalAllocatedAmount - totalCollectedAmount;

            // --- 5. Recent Activities ---
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

            // --- 6. Buildings Summary ---
            var buildingsSummary = buildings.Select(b =>
            {
                // Calculate occupancy for this specific building using the HashSet
                var bOccupied = b.Flats?.Count(f => occupiedFlatIds.Contains(f.Id)) ?? 0;
                var bTotalFlats = b.Flats?.Count ?? 0;

                return new BuildingSummaryViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    TotalFlats = bTotalFlats,
                    OccupiedFlats = bOccupied,
                    // Vacant is calculated in ViewModel as Total - Occupied
                    TotalBills = b.CommonBills?.Sum(cb => cb.TotalAmount) ?? 0,
                    TotalPayments = b.ExpensePayments?.Sum(ep => ep.Amount) ?? 0,
                    // Balance = Billed - Spent (Theoretical) OR Collected - Spent (Actual). 
                    // Keeping original logic (Billed - Spent) for row consistency, or update if needed.
                    Balance = (b.CommonBills?.Sum(cb => cb.TotalAmount) ?? 0) - (b.ExpensePayments?.Sum(ep => ep.Amount) ?? 0)
                };
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
                TotalStaffs = staffs.Count,
                PendingApprovals = pendingUsers.Count,
                TotalTenants = tenants.Count,

                // Flats Data
                TotalFlats = totalFlats,
                OccupiedFlats = occupiedFlatsCount,
                VacantFlats = totalFlats - occupiedFlatsCount,
                FlatsWithOwners = flatsWithOwners,
                FlatsWithoutOwners = totalFlats - flatsWithOwners,

                // Financial Data
                TotalBillsGenerated = totalBillsAmount,
                TotalPaymentsMade = totalPaymentsAmount,
                TotalAmountCollected = totalCollectedAmount,
                TotalPendingCollection = totalPendingAmount,
                // Balance = Money In (Collected) - Money Out (Payments)
                OverallBalance = totalCollectedAmount - totalPaymentsAmount,

                // Recent Activities
                RecentBills = recentBills,
                RecentPayments = recentPayments
            };

            return View(viewModel);
        }
    }
}