using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class ExpenseAllocationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpenseAllocationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(Guid? commonBillId)
        {
            if (commonBillId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var commonBill = await _context.CommonBills
                                            .Include(b => b.Building)
                                            .FirstOrDefaultAsync(b => b.Id == commonBillId);

            if (commonBill == null || (commonBill.BuildingId != user.BuildingId && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            var allocations = await _context.ExpenseAllocations
                                            .Include(a => a.Owner)
                                            .Where(a => a.CommonBillId == commonBillId)
                                            .ToListAsync();

            ViewData["CommonBillName"] = commonBill.Name;
            ViewData["BuildingId"] = commonBill.BuildingId;
            return View(allocations);
        }
    }
}
