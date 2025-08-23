using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "President,SuperAdmin")]
    public class ExpensePaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpensePaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ExpensePayment/Index/{buildingId}
        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var payments = await _context.ExpensePayments
                                         .Include(p => p.CommonBill)
                                         .Where(p => p.BuildingId == buildingId)
                                         .OrderByDescending(p => p.PaymentDate)
                                         .ToListAsync();

            ViewData["BuildingId"] = buildingId;
            return View(payments);
        }

        // GET: ExpensePayment/Create/{buildingId}
        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            // Populate a dropdown list of unpaid bills for the President to select
            var unpaidBills = await _context.CommonBills
                                            .Where(b => b.BuildingId == buildingId)
                                            .Where(b => !_context.ExpensePayments.Any(p => p.CommonBillId == b.Id))
                                            .ToListAsync();

            ViewData["CommonBillId"] = new SelectList(unpaidBills, "Id", "Name");
            ViewData["BuildingId"] = buildingId;
            return View();
        }

        // POST: ExpensePayment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,PaymentDate,Amount,Notes,BuildingId,CommonBillId")] ExpensePayment payment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != payment.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            if (ModelState.IsValid)
            {
                await _context.AddAsync(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { buildingId = payment.BuildingId });
            }

            // Repopulate the dropdown if model state is invalid
            var unpaidBills = await _context.CommonBills
                                            .Where(b => b.BuildingId == payment.BuildingId)
                                            .Where(b => !_context.ExpensePayments.Any(p => p.CommonBillId == b.Id))
                                            .ToListAsync();
            ViewData["CommonBillId"] = new SelectList(unpaidBills, "Id", "Name", payment.CommonBillId);
            ViewData["BuildingId"] = payment.BuildingId;
            return View(payment);
        }
    }
}
