using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Features.President.Services;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.President;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Controllers;

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
