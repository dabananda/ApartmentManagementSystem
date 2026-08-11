using ApartmentManagementSystem.Features.Administration.Repositories;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Administration.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Administration.Services;

public sealed class SuperAdminDashboardService(ISuperAdminDashboardRepository dashboard, UserManager<ApplicationUser> users) : ISuperAdminDashboardService
{
    public async Task<SuperAdminDashboardViewModel> GetAsync(CancellationToken cancellationToken = default)
    {
        var buildings = await dashboard.GetBuildingsAsync(cancellationToken);
        var allUsers = await users.Users.ToListAsync(cancellationToken);
        var superAdmins = await users.GetUsersInRoleAsync("SuperAdmin");
        var presidents = await users.GetUsersInRoleAsync("President");
        var owners = await users.GetUsersInRoleAsync("Owner");
        var tenants = await users.GetUsersInRoleAsync("Tenant");
        var staffs = await users.GetUsersInRoleAsync("Staff");
        var pendingUsers = await users.GetUsersInRoleAsync("User");
        var occupiedFlatIds = await dashboard.GetOccupiedFlatIdsAsync(cancellationToken);
        var (totalFlats, flatsWithOwners) = await dashboard.GetFlatCountsAsync(cancellationToken);
        var (bills, payments, collected, allocated) = await dashboard.GetFinancialTotalsAsync(cancellationToken);
        return new SuperAdminDashboardViewModel
        {
            TotalBuildings = buildings.Count,
            BuildingsSummary = buildings.Select(building => new BuildingSummaryViewModel { Id = building.Id, Name = building.Name, Address = building.Address, TotalFlats = building.Flats?.Count ?? 0, OccupiedFlats = building.Flats?.Count(flat => occupiedFlatIds.Contains(flat.Id)) ?? 0, TotalBills = building.CommonBills?.Sum(bill => bill.TotalAmount) ?? 0, TotalPayments = building.ExpensePayments?.Sum(payment => payment.Amount) ?? 0, Balance = (building.CommonBills?.Sum(bill => bill.TotalAmount) ?? 0) - (building.ExpensePayments?.Sum(payment => payment.Amount) ?? 0) }).ToList(),
            TotalUsers = allUsers.Count, TotalSuperAdmins = superAdmins.Count, TotalPresidents = presidents.Count, TotalOwners = owners.Count, TotalStaffs = staffs.Count, PendingApprovals = pendingUsers.Count, TotalTenants = tenants.Count,
            TotalFlats = totalFlats, OccupiedFlats = occupiedFlatIds.Count, VacantFlats = totalFlats - occupiedFlatIds.Count, FlatsWithOwners = flatsWithOwners, FlatsWithoutOwners = totalFlats - flatsWithOwners,
            TotalBillsGenerated = bills, TotalPaymentsMade = payments, TotalAmountCollected = collected, TotalPendingCollection = allocated - collected, OverallBalance = collected - payments,
            RecentBills = (await dashboard.GetRecentBillsAsync(cancellationToken)).ToList(), RecentPayments = (await dashboard.GetRecentPaymentsAsync(cancellationToken)).ToList()
        };
    }
}
