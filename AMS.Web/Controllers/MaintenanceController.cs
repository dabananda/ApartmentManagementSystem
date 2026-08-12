using AMS.Application.Features.Maintenance.Commands;
using AMS.Application.Features.Maintenance.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize(Roles = Roles.PresidentOrSuperAdmin)]
public class MaintenanceController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;

    public MaintenanceController(UserManager<ApplicationUser> userManager, IMediator mediator)
    {
        _userManager = userManager;
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(string status = "Open")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.BuildingId == null) return Forbid();
        var buildingId = user.BuildingId.Value;

        var items = await _mediator.Send(new GetMaintenanceTicketsForBuildingQuery(buildingId, status));

        ViewBag.SelectedStatus = status;
        return View(items);
    }

    public IActionResult Create() => View(new MaintenanceTicket());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceTicket model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.BuildingId == null) return Forbid();
        var buildingId = user.BuildingId.Value;

        if (!ModelState.IsValid) return View(model);

        await _mediator.Send(new CreateMaintenanceTicketCommand(model, buildingId));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Advance(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.BuildingId == null) return Forbid();
        var buildingId = user.BuildingId.Value;

        var ticket = await _mediator.Send(new AdvanceMaintenanceTicketCommand(id, buildingId));

        if (ticket == null) return NotFound();

        return RedirectToAction(nameof(Index), new { status = ticket.Status });
    }
}
