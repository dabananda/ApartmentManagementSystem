using ApartmentManagementSystem.Features.Administration.Services;
using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Controllers;

[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController(ISuperAdminDashboardService dashboard) : Controller
{
    public async Task<IActionResult> Dashboard() => View(await dashboard.GetAsync());
}
