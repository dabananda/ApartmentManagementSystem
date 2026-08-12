using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Administration.Repositories;
using ApartmentManagementSystem.Features.Administration.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Administration.Services;

public sealed class SuperAdminDashboardService(
    ISuperAdminDashboardRepository dashboard,
    UserManager<ApplicationUser> users) : ISuperAdminDashboardService
{
    public async Task<SuperAdminDashboardViewModel> GetAsync(CancellationToken cancellationToken = default)
    {
        // Load all data concurrently where possible
        var buildings = await dashboard.GetBuildingsAsync(cancellationToken);
        var allUsers = await users.Users.ToListAsync(cancellationToken);

        var superAdmins  = await users.GetUsersInRoleAsync(Roles.SuperAdmin);
        var presidents   = await users.GetUsersInRoleAsync(Roles.President);
        var owners       = await users.GetUsersInRoleAsync(Roles.Owner);
        var tenants      = await users.GetUsersInRoleAsync(Roles.Tenant);
        var staffs       = await users.GetUsersInRoleAsync(Roles.Staff);
        var pendingUsers = await users.GetUsersInRoleAsync(Roles.User);

        var occupiedFlatIds = await dashboard.GetOccupiedFlatIdsAsync(cancellationToken);
        var (totalFlats, flatsWithOwners) = await dashboard.GetFlatCountsAsync(cancellationToken);
        var (bills, payments, collected, allocated) = await dashboard.GetFinancialTotalsAsync(cancellationToken);
        var recentBills    = (await dashboard.GetRecentBillsAsync(cancellationToken)).ToList();
        var recentPayments = (await dashboard.GetRecentPaymentsAsync(cancellationToken)).ToList();

        var buildingsSummary = buildings.Select(b => new BuildingSummaryViewModel
        {
            Id           = b.Id,
            Name         = b.Name,
            Address      = b.Address,
            TotalFlats   = b.Flats?.Count ?? 0,
            OccupiedFlats = b.Flats?.Count(f => occupiedFlatIds.Contains(f.Id)) ?? 0,
            TotalBills   = b.CommonBills?.Sum(bill => bill.TotalAmount) ?? 0,
            TotalPayments = b.ExpensePayments?.Sum(p => p.Amount) ?? 0,
            Balance      = (b.CommonBills?.Sum(bill => bill.TotalAmount) ?? 0)
                         - (b.ExpensePayments?.Sum(p => p.Amount) ?? 0)
        }).ToList();

        return new SuperAdminDashboardViewModel
        {
            // Buildings
            TotalBuildings   = buildings.Count,
            BuildingsSummary = buildingsSummary,

            // Users
            TotalUsers      = allUsers.Count,
            TotalSuperAdmins = superAdmins.Count,
            TotalPresidents  = presidents.Count,
            TotalOwners      = owners.Count,
            TotalStaffs      = staffs.Count,
            TotalTenants     = tenants.Count,
            PendingApprovals = pendingUsers.Count,

            // Flats
            TotalFlats        = totalFlats,
            OccupiedFlats     = occupiedFlatIds.Count,
            VacantFlats       = totalFlats - occupiedFlatIds.Count,
            FlatsWithOwners   = flatsWithOwners,
            FlatsWithoutOwners = totalFlats - flatsWithOwners,

            // Financials
            TotalBillsGenerated   = bills,
            TotalPaymentsMade     = payments,
            TotalAmountCollected  = collected,
            TotalPendingCollection = allocated - collected,
            OverallBalance        = collected - payments,

            // Recent activity
            RecentBills    = recentBills,
            RecentPayments = recentPayments
        };
    }
}
