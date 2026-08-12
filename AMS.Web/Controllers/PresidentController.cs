using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Application.Features.President.Services;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.President.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers
{
[Authorize(Roles = Roles.PresidentOrSuperAdmin)]
public class PresidentController(IPresidentDashboardService dashboard) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        if (!Guid.TryParse(User.FindFirst("building_id")?.Value, out var buildingId))
        {
            TempData["DashboardNotice"] = "Your account isn’t linked to a building yet. Please contact a Super Admin.";
            return View(new PresidentDashboardViewModel());
        }
        return View(await dashboard.GetAsync(buildingId));
    }
}

}