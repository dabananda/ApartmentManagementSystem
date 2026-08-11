using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Administration.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ApartmentManagementSystem.Features.Administration.Repositories;

public interface IUserManagementRepository
{
    /// <summary>Returns buildings as SelectListItems, optionally restricted to a single building.</summary>
    Task<List<SelectListItem>> GetBuildingSelectItemsAsync(Guid? restrictToBuildingId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns a single building as a SelectListItem list (for President users who can only see their building).</summary>
    Task<SelectListItem?> GetBuildingSelectItemAsync(Guid buildingId, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a user has billing history (TenantBills) or active tenant assignments.</summary>
    Task<bool> HasBlockingRecordsAsync(string userId, CancellationToken cancellationToken = default);
}
