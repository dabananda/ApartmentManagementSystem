using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "President,SuperAdmin")]
    public class ExpenseAllocationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpenseAllocationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ExpenseAllocation/Index/{expenseId}
        public async Task<IActionResult> Index(Guid? expenseId)
        {
            if (expenseId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var expense = await _context.CommonExpenses
                                        .Include(e => e.Building)
                                        .FirstOrDefaultAsync(e => e.Id == expenseId);

            // Security check: The user's BuildingId must match the expense's BuildingId
            if (expense == null || (expense.BuildingId != user.BuildingId && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            var allocations = await _context.ExpenseAllocations
                                            .Include(a => a.Owner)
                                            .Where(a => a.CommonExpenseId == expenseId)
                                            .ToListAsync();

            ViewData["ExpenseName"] = expense.Name;
            ViewData["BuildingId"] = expense.BuildingId;
            return View(allocations);
        }

        // POST: ExpenseAllocation/MarkAsPaid/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(Guid id)
        {
            var allocation = await _context.ExpenseAllocations
                                           .Include(a => a.CommonExpense)
                                           .ThenInclude(e => e.Building)
                                           .FirstOrDefaultAsync(a => a.Id == id);

            if (allocation == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            // Security check: The user's BuildingId must match the allocation's BuildingId
            if (allocation.CommonExpense?.BuildingId != user.BuildingId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            allocation.IsPaid = true;
            allocation.PaymentDate = DateTime.Now;
            _context.Update(allocation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { expenseId = allocation.CommonExpenseId });
        }
    }
}
