using ApartmentManagementSystem.Application.Interfaces.Administration;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Features.Administration;

[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController(ISuperAdminDashboardService dashboard) : Controller
{
    public async Task<IActionResult> Dashboard() => View(await dashboard.GetAsync());
}
