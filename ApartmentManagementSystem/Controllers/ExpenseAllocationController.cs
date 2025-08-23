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

        // GET: ExpenseAllocation/Index/{commonBillId}
        public async Task<IActionResult> Index(Guid? commonBillId)
        {
            if (commonBillId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var commonBill = await _context.CommonBills
                                            .Include(b => b.Building)
                                            .FirstOrDefaultAsync(b => b.Id == commonBillId);

            // Security check: The user's BuildingId must match the bill's BuildingId
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

        // POST: ExpenseAllocation/MarkAsPaid/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(Guid id)
        {
            var allocation = await _context.ExpenseAllocations
                                           .Include(a => a.CommonBill)
                                           .FirstOrDefaultAsync(a => a.Id == id);

            if (allocation == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            // Security check: The user's BuildingId must match the allocation's bill's BuildingId
            if (allocation.CommonBill?.BuildingId != user.BuildingId && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            allocation.IsPaid = true;
            allocation.PaymentDate = DateTime.Now;
            _context.Update(allocation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { commonBillId = allocation.CommonBillId });
        }
    }
}
