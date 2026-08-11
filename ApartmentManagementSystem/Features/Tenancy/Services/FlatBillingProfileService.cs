using ApartmentManagementSystem.Features.Tenancy.Repositories;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Flat;
namespace ApartmentManagementSystem.Features.Tenancy.Services;
public sealed class FlatBillingProfileService(IFlatBillingProfileRepository profiles) : IFlatBillingProfileService
{
    public Task<IReadOnlyList<FlatProfileRow>> GetRowsAsync(string? ownerId) => profiles.GetRowsAsync(ownerId);
    public Task<Flat?> GetFlatAsync(Guid flatId) => profiles.GetFlatAsync(flatId);
    public Task<FlatBillingProfile?> GetProfileAsync(Guid flatId) => profiles.GetProfileAsync(flatId);
    public async Task SaveAsync(FlatBillingProfile profile)
    {
        var existing = await profiles.GetProfileAsync(profile.FlatId);
        if (existing is null) existing = new FlatBillingProfile { FlatId = profile.FlatId };
        existing.Title = string.IsNullOrWhiteSpace(profile.Title) ? "Monthly Rent" : profile.Title; existing.MonthlyAmount = profile.MonthlyAmount; existing.DueDayOfMonth = profile.DueDayOfMonth <= 0 ? 1 : profile.DueDayOfMonth; existing.IsActive = profile.IsActive;
        await profiles.SaveProfileAsync(existing);
        if (!existing.IsActive) return;
        var today = DateTime.Today; var assignment = await profiles.GetCurrentAssignmentAsync(profile.FlatId, today); if (assignment is null) return;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1); if (new DateTime(assignment.StartDate.Year, assignment.StartDate.Month, 1) > firstOfMonth) return;
        if (!await profiles.TenantBillExistsAsync(profile.FlatId, assignment.TenantUserId, firstOfMonth)) await profiles.AddTenantBillAsync(new TenantBill { FlatId = profile.FlatId, TenantUserId = assignment.TenantUserId, Title = string.IsNullOrWhiteSpace(existing.Title) ? "Monthly Rent" : existing.Title, BillDate = firstOfMonth, Amount = existing.MonthlyAmount });
    }
}
