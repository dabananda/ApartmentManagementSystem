using AMS.Application.Features.President.DTOs;
using AMS.Application.Features.President.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize(Roles = Roles.PresidentOrSuperAdmin)]
public class PresidentController(IMediator mediator) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        if (!Guid.TryParse(User.FindFirst("building_id")?.Value, out var buildingId))
        {
            TempData["DashboardNotice"] = "Your account isn’t linked to a building yet. Please contact a Super Admin.";
            return View(new PresidentDashboardViewModel());
        }
        return View(await mediator.Send(new GetPresidentDashboardQuery(buildingId)));
    }
}
