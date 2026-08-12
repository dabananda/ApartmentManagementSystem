using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ApartmentManagementSystem.Features.Shared;

/// <summary>
/// Extension methods shared across MVC controllers to eliminate repetitive patterns.
/// </summary>
public static class ControllerBaseExtensions
{
    /// <summary>
    /// Resolves the current authenticated user and derives their role/building context
    /// in a single, consistent call — replacing duplicated GetUserAsync + IsInRole blocks.
    /// </summary>
    public static async Task<CallerContext?> GetCallerContextAsync(
        this ControllerBase ctrl,
        UserManager<ApplicationUser> userManager)
    {
        var me = await userManager.GetUserAsync(ctrl.User);
        if (me is null) return null;

        var isSuperAdmin = ctrl.User.IsInRole(Roles.SuperAdmin);
        return new CallerContext(me, isSuperAdmin, me.BuildingId);
    }

    /// <summary>
    /// Builds an absolute URL for the given action/controller — replaces the duplicated
    /// private <c>AbsoluteUrl()</c> helper that appeared in OwnerBillingController and TenantRentController.
    /// </summary>
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
