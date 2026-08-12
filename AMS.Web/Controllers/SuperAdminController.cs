using AMS.Application.Features.Administration.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController(IMediator mediator) : Controller
{
    public async Task<IActionResult> Dashboard() => View(await mediator.Send(new GetSuperAdminDashboardQuery()));
}
