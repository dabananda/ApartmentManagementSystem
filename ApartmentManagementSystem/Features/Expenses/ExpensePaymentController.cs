using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ApartmentManagementSystem.Features.Expenses.Services;

namespace ApartmentManagementSystem.Features.Expenses
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class ExpensePaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExpensePaymentService _payments;

        public ExpensePaymentController(UserManager<ApplicationUser> userManager, IExpensePaymentService payments)
        {
            _userManager = userManager;
            _payments = payments;
        }

        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var payments = await _payments.GetForBuildingAsync(buildingId.Value);

            ViewData["BuildingId"] = buildingId;
            return View(payments);
        }

        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var unpaidBills = (await _payments.GetOutstandingBillsAsync(buildingId.Value))
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,PaymentDate,Amount,Notes,BuildingId,CommonBillId")] ExpensePayment payment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != payment.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            if (ModelState.IsValid)
            {
                var remainingAmount = await _payments.GetRemainingAmountAsync(payment.CommonBillId);

                if (payment.Amount > remainingAmount)
                {
                    ModelState.AddModelError("Amount", $"Payment amount cannot exceed the remaining balance of {remainingAmount:C}.");
                }

                if (ModelState.IsValid)
                {
                    await _payments.RecordAsync(payment);
                    TempData["Success"] = "Payment recorded successfully.";
                    return RedirectToAction(nameof(Index), new { buildingId = payment.BuildingId });
                }
            }

            var unpaidBillsOnFail = (await _payments.GetOutstandingBillsAsync(payment.BuildingId))
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
        
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();
            var payment = await _payments.GetAsync(id.Value);
            if (payment == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != payment.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();
            return View(payment);
        }
    }
}
