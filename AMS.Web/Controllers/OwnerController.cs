using AMS.Application.Features.Owner.DTOs;
using AMS.Application.Features.Owner.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
public class OwnerController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;

    public OwnerController(UserManager<ApplicationUser> userManager, IMediator mediator)
    {
        _userManager = userManager;
        _mediator = mediator;
    }

    private async Task<(ApplicationUser me, bool isSuperAdmin)> GetCallerInfoAsync()
    {
        var me = await _userManager.GetUserAsync(User);
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        return (me!, isSuperAdmin);
    }

    public async Task<IActionResult> Dashboard()
    {
        var (me, _) = await GetCallerInfoAsync();
        if (me == null) return Forbid();

        var vm = await _mediator.Send(new GetOwnerDashboardQuery(me.Id));
        return View(vm);
    }

    public async Task<IActionResult> OwnedFlats(string? ownerId = null)
    {
        var (me, _) = await GetCallerInfoAsync();
        if (me == null) return Forbid();

        var targetOwnerId = User.IsInRole("Owner") ? me.Id : (ownerId ?? me.Id);

        var rows = await _mediator.Send(new GetOwnedFlatsQuery(targetOwnerId));
        ViewBag.TargetOwnerId = targetOwnerId;
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> CommonBills(string? ownerId = null)
    {
        var (me, _) = await GetCallerInfoAsync();
        if (me == null) return Forbid();

        var targetOwnerId = User.IsInRole("Owner") ? me.Id : (ownerId ?? me.Id);

        var restrictToBuildingId = (User.IsInRole("President") && me.BuildingId != null) ? me.BuildingId : null;

        var page = await _mediator.Send(new GetOwnerCommonBillsPageQuery(targetOwnerId, restrictToBuildingId));

        // If allocations were 0, service returns an empty model with the user's name
        if (page == null)
        {
            var owner = await _userManager.FindByIdAsync(targetOwnerId);
            page = new OwnerBillsPage
            {
                OwnerId = targetOwnerId,
                OwnerName = owner?.Fullname ?? owner?.UserName ?? "(owner)",
                Bills = [],
                BuildingId = Guid.Empty,
                History = []
            };
        }

        return View(page);
    }
}
