using AMS.Application.Interfaces.Administration;
using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Web.Features.Payments;
using AMS.Application.Features.Home.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Features.Administration;

[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController(ISuperAdminDashboardService dashboard) : Controller
{
    public async Task<IActionResult> Dashboard() => View(await dashboard.GetAsync());
}
