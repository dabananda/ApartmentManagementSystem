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
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Report/BuildingFinancialReport
        public async Task<IActionResult> BuildingFinancialReport()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null)
            {
                return Forbid();
            }

            var buildingId = user.BuildingId.Value;

            var building = await _context.Buildings
                                         .Include(b => b.CommonBills)
                                         .Include(b => b.ExpensePayments)
                                         .FirstOrDefaultAsync(b => b.Id == buildingId);

            if (building == null) return NotFound();

            // Calculate totals
            var totalBills = building.CommonBills.Sum(b => b.TotalAmount);
            var totalPayments = building.ExpensePayments.Sum(p => p.Amount);

            // Calculate total collected by summing paid allocations
            var totalCollected = await _context.ExpenseAllocations
                                               .Where(a => a.CommonBill.BuildingId == buildingId && a.IsPaid)
                                               .SumAsync(a => a.AmountDue);

            // Get all allocations to list on the dashboard
            var allocations = await _context.ExpenseAllocations
                                            .Include(a => a.Owner)
                                            .Include(a => a.CommonBill)
                                            .Where(a => a.CommonBill.BuildingId == buildingId)
                                            .ToListAsync();

            var viewModel = new ReportDashboardViewModel
            {
                BuildingName = building.Name,
                TotalBills = totalBills,
                TotalCollected = totalCollected,
                TotalPayments = totalPayments,
                Balance = totalCollected - totalPayments,
                Allocations = allocations
            };

            return View(viewModel);
        }
    }
}