using AMS.Application.Features.Expenses.Commands;
using AMS.Application.Features.Expenses.DTOs;
using AMS.Application.Features.Expenses.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Web.Controllers;

[Authorize(Roles = Roles.PresidentOrSuperAdmin)]
public class ExpensePaymentController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;

    public ExpensePaymentController(
        UserManager<ApplicationUser> userManager,
        IMediator mediator)
    {
        _userManager = userManager;
        _mediator = mediator;
    }

    private async Task<bool> IsAuthorizedForBuildingAsync(Guid buildingId)
    {
        if (User.IsInRole(Roles.SuperAdmin)) return true;
        var ctx = await this.GetCallerContextAsync(_userManager);
        return ctx?.BuildingId == buildingId;
    }

    private async Task<List<SelectListItem>> GetOutstandingBillSelectItemsAsync(Guid buildingId) =>
        (await _mediator.Send(new GetOutstandingBillsQuery(buildingId)))
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

        var payments = await _mediator.Send(new GetExpensePaymentsForBuildingQuery(buildingId.Value));

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
            var remainingAmount = await _mediator.Send(new GetRemainingExpenseAmountQuery(model.CommonBillId));
            if (model.Amount > remainingAmount)
                ModelState.AddModelError("Amount", $"Payment amount cannot exceed the remaining balance of {remainingAmount:C}.");
        }

        if (ModelState.IsValid)
        {
            var payment = model.ToEntity();
            await _mediator.Send(new RecordExpensePaymentCommand(payment));
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

        var payment = await _mediator.Send(new GetExpensePaymentByIdQuery(id.Value));
        if (payment == null) return NotFound();
        if (!await IsAuthorizedForBuildingAsync(payment.BuildingId)) return Forbid();

        return View(payment);
    }
}
