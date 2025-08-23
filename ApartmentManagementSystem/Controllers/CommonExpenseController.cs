using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "President,SuperAdmin")]
    public class CommonExpenseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommonExpenseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: CommonExpense/Index/{buildingId}
        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (user.BuildingId != buildingId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            var expenses = await _context.CommonExpenses
                                         .Where(e => e.BuildingId == buildingId)
                                         .OrderByDescending(e => e.ExpenseDate)
                                         .ToListAsync();

            ViewData["BuildingId"] = buildingId;
            return View(expenses);
        }

        // GET: CommonExpense/Create/{buildingId}
        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (user.BuildingId != buildingId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            ViewData["BuildingId"] = buildingId;
            return View();
        }

        // POST: CommonExpense/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ExpenseDate,Amount,Notes,BuildingId")] CommonExpense expense)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (user.BuildingId != expense.BuildingId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                await _context.AddAsync(expense);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { buildingId = expense.BuildingId });
            }

            ViewData["BuildingId"] = expense.BuildingId;
            return View(expense);
        }
    }
}
