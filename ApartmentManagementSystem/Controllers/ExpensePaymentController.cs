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

            // Get a list of all payments for the building, grouped by CommonBillId
            var paidAmounts = await _context.ExpensePayments
                                            .Where(p => p.BuildingId == buildingId)
                                            .GroupBy(p => p.CommonBillId)
                                            .Select(g => new { CommonBillId = g.Key, PaidAmount = g.Sum(p => p.Amount) })
                                            .ToListAsync();

            // Get all common bills for the building and calculate the outstanding amount
            var bills = await _context.CommonBills
                                      .Where(b => b.BuildingId == buildingId)
                                      .ToListAsync();

            var unpaidBills = bills.Select(b => new
            {
                b.Id,
                b.Name,
                Outstanding = b.TotalAmount - (paidAmounts.FirstOrDefault(p => p.CommonBillId == b.Id)?.PaidAmount ?? 0)
            })
            .Where(b => b.Outstanding > 0)
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.Name} (Outstanding: {b.Outstanding:C})"
            })
            .ToList();

            ViewData["CommonBillId"] = unpaidBills;
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
                // Calculate the remaining balance of the bill
                var paidSoFar = await _context.ExpensePayments
                                              .Where(p => p.CommonBillId == payment.CommonBillId)
                                              .SumAsync(p => p.Amount);

                var bill = await _context.CommonBills.FindAsync(payment.CommonBillId);
                var remainingAmount = bill.TotalAmount - paidSoFar;

                if (payment.Amount > remainingAmount)
                {
                    ModelState.AddModelError("Amount", $"Payment amount cannot exceed the remaining balance of {remainingAmount:C}.");
                }

                if (ModelState.IsValid)
                {
                    _context.Add(payment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Payment recorded successfully.";
                    return RedirectToAction(nameof(Index), new { buildingId = payment.BuildingId });
                }
            }

            // Repopulate the dropdown if model state is invalid
            var paidAmountsOnFail = await _context.ExpensePayments
                                                  .Where(p => p.BuildingId == payment.BuildingId)
                                                  .GroupBy(p => p.CommonBillId)
                                                  .Select(g => new { CommonBillId = g.Key, PaidAmount = g.Sum(p => p.Amount) })
                                                  .ToListAsync();

            var billsOnFail = await _context.CommonBills
                                            .Where(b => b.BuildingId == payment.BuildingId)
                                            .ToListAsync();

            var unpaidBillsOnFail = billsOnFail.Select(b => new
            {
                b.Id,
                b.Name,
                Outstanding = b.TotalAmount - (paidAmountsOnFail.FirstOrDefault(p => p.CommonBillId == b.Id)?.PaidAmount ?? 0)
            })
            .Where(b => b.Outstanding > 0)
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.Name} (Outstanding: {b.Outstanding:C})"
            })
            .ToList();

            ViewData["CommonBillId"] = unpaidBillsOnFail;
            ViewData["BuildingId"] = payment.BuildingId;
            return View(payment);
        }
    }
}