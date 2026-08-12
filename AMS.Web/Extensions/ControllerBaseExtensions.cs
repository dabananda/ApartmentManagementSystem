using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AMS.Web.Extensions;

public static class ControllerBaseExtensions
{
    public static async Task<CallerContext?> GetCallerContextAsync(
    this ControllerBase ctrl,
    UserManager<ApplicationUser> userManager)
    {
        var me = await userManager.GetUserAsync(ctrl.User);
        if (me is null) return null;

        var isSuperAdmin = ctrl.User.IsInRole(Roles.SuperAdmin);
        return new CallerContext(me, isSuperAdmin, me.BuildingId);
    }

    public static string BuildAbsoluteUrl(
    this ControllerBase ctrl,
    IUrlHelperFactory urlHelperFactory,
    IActionContextAccessor actionContextAccessor,
    string action,
    string controller,
    object? routeValues = null)
    {
        var actionContext = actionContextAccessor.ActionContext!;
        var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);
        return urlHelper.Action(action, controller, routeValues, actionContext.HttpContext.Request.Scheme)!;
    }
}
