using AMS.Domain.Entities;

namespace AMS.Web.Extensions;

public sealed record CallerContext(ApplicationUser Me, bool IsSuperAdmin, Guid? BuildingId)
{
    public bool IsAuthorizedForBuilding(Guid? buildingId) =>
    IsSuperAdmin || buildingId == null || BuildingId == buildingId;
}
