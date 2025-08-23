using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
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

            // Calculate total expenses for the building
            var totalExpenses = await _context.CommonExpenses
                                              .Where(e => e.BuildingId == buildingId)
                                              .SumAsync(e => e.Amount);

            // Calculate total collected amount from expense allocations
            var totalCollected = await _context.ExpenseAllocations
                                               .Include(a => a.CommonExpense)
                                               .Where(a => a.CommonExpense.BuildingId == buildingId && a.IsPaid)
                                               .SumAsync(a => a.AmountDue);

            var building = await _context.Buildings.FindAsync(buildingId);

            ViewBag.BuildingName = building?.Name ?? "Building";
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.Balance = totalCollected - totalExpenses;

            return View();
        }
    }
}