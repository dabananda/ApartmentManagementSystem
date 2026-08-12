using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Application.Features.President.Services;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.President.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Features.President;

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
