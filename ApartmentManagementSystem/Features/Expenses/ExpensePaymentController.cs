using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Expenses.Services;
using ApartmentManagementSystem.Application.Features.Expenses.DTOs;
using ApartmentManagementSystem.Features.Shared;
using ApartmentManagementSystem.Application.Features.Buildings.Queries;
using ApartmentManagementSystem.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ApartmentManagementSystem.Features.Expenses
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class ExpensePaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExpensePaymentService _payments;
        private readonly IMediator _mediator;
        private readonly ICommonBillService _bills;

        public ExpensePaymentController(
            UserManager<ApplicationUser> userManager,
            IExpensePaymentService payments,
            IMediator mediator,
            ICommonBillService bills)
        {
            _userManager = userManager;
            _payments = payments;
            _mediator = mediator;
            _bills = bills;
        }

        /// <summary>Returns true if the current user is authorised to manage payments for <paramref name="buildingId"/>.</summary>
        private async Task<bool> IsAuthorizedForBuildingAsync(Guid buildingId)
        {
            if (User.IsInRole(Roles.SuperAdmin)) return true;
            var ctx = await this.GetCallerContextAsync(_userManager);
            return ctx?.BuildingId == buildingId;
        }

        private async Task<List<SelectListItem>> GetOutstandingBillSelectItemsAsync(Guid buildingId) =>
            (await _payments.GetOutstandingBillsAsync(buildingId))
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"{b.Name} (Outstanding: {b.Outstanding:C})"
                })
                .ToList();

        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(buildingId.Value)) return Forbid();

            var payments = await _payments.GetForBuildingAsync(buildingId.Value);

            ViewData["BuildingId"] = buildingId;
            return View(payments);
        }

        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(buildingId.Value)) return Forbid();

            ViewData["CommonBillId"] = await GetOutstandingBillSelectItemsAsync(buildingId.Value);
            ViewData["BuildingId"] = buildingId;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExpensePaymentCreateViewModel model)
        {
            if (!await IsAuthorizedForBuildingAsync(model.BuildingId)) return Forbid();

            if (ModelState.IsValid)
            {
                var remainingAmount = await _payments.GetRemainingAmountAsync(model.CommonBillId);
                if (model.Amount > remainingAmount)
                    ModelState.AddModelError("Amount", $"Payment amount cannot exceed the remaining balance of {remainingAmount:C}.");
            }

            if (ModelState.IsValid)
            {
                var payment = model.ToEntity();
                await _payments.RecordAsync(payment);
                TempData["Success"] = "Payment recorded successfully.";
                return RedirectToAction(nameof(Index), new { buildingId = model.BuildingId });
            }

            ViewData["CommonBillId"] = await GetOutstandingBillSelectItemsAsync(model.BuildingId);
            ViewData["BuildingId"] = model.BuildingId;
            return View(model);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var payment = await _payments.GetAsync(id.Value);
            if (payment == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(payment.BuildingId)) return Forbid();

            return View(payment);
        }
    }
}
