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

        // GET: CommonExpense/Details/{id}
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var expense = await _context.CommonExpenses
                .Include(e => e.Building)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (expense == null)
            {
                return NotFound();
            }

            // Security check
            if (expense.Building.Id != user.BuildingId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            return View(expense);
        }

        // GET: CommonExpense/Edit/{id}
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var expense = await _context.CommonExpenses
                .Include(e => e.Building)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (expense == null)
            {
                return NotFound();
            }

            // Security check
            if (expense.Building.Id != user.BuildingId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            return View(expense);
        }

        // POST: CommonExpense/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,ExpenseDate,Amount,Notes,BuildingId")] CommonExpense expense)
        {
            if (id != expense.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                // Final security check before saving
                if (expense.BuildingId != user.BuildingId && !User.IsInRole("SuperAdmin"))
                {
                    return Forbid();
                }

                try
                {
                    _context.Update(expense);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CommonExpenses.Any(e => e.Id == expense.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { buildingId = expense.BuildingId });
            }
            return View(expense);
        }

        // GET: CommonExpense/Delete/{id}
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var expense = await _context.CommonExpenses
                .Include(e => e.Building)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (expense == null)
            {
                return NotFound();
            }

            // Security check
            if (expense.Building.Id != user.BuildingId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            return View(expense);
        }

        // POST: CommonExpense/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var expense = await _context.CommonExpenses.FindAsync(id);
            if (expense != null)
            {
                var user = await _userManager.GetUserAsync(User);

                // Final security check before deleting
                if (expense.BuildingId != user.BuildingId && !User.IsInRole("SuperAdmin"))
                {
                    return Forbid();
                }

                _context.CommonExpenses.Remove(expense);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { buildingId = expense.BuildingId });
            }

            return NotFound();
        }
    }
}
