using AMS.Application.Features.Expenses.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize(Roles = Roles.PresidentOrSuperAdmin)]
public class ExpenseAllocationController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;

    public ExpenseAllocationController(UserManager<ApplicationUser> userManager, IMediator mediator)
    {
        _userManager = userManager;
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(Guid? commonBillId)
    {
        if (commonBillId == null) return NotFound();

        var ctx = await this.GetCallerContextAsync(_userManager);
        if (ctx == null) return Forbid();

        var result = await _mediator.Send(new GetExpenseAllocationQuery(commonBillId.Value));
        var commonBill = result.CommonBill;

        if (commonBill == null || !ctx.IsAuthorizedForBuilding(commonBill.BuildingId))
            return Forbid();

        ViewData["CommonBillName"] = commonBill.Name;
        ViewData["BuildingId"] = commonBill.BuildingId;
        return View(result.Allocations);
    }
}
