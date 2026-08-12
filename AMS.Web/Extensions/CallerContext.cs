using AMS.Domain.Entities;

namespace AMS.Web.Extensions;

/// <summary>
/// Encapsulates the identity context of the currently authenticated user for a single request.
/// Obtained via <see cref="ControllerBaseExtensions.GetCallerContextAsync"/>.
/// </summary>
public sealed record CallerContext(ApplicationUser Me, bool IsSuperAdmin, Guid? BuildingId)
{
    /// <summary>
    /// Returns true if the caller is authorised to act on resources scoped to <paramref name="buildingId"/>.
    /// SuperAdmins bypass all building restrictions.
    /// </summary>
    public bool IsAuthorizedForBuilding(Guid? buildingId) =>
        IsSuperAdmin || buildingId == null || BuildingId == buildingId;
}
