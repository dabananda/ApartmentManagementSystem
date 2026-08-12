using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;
using AMS.Domain.Entities;
using AMS.Application.Features.Tenancy.DTOs;

namespace AMS.Application.Features.Tenancy.Commands;

public record SaveFlatBillingProfileCommand(FlatBillingProfile Profile) : IRequest;

public class SaveFlatBillingProfileCommandHandler(IFlatBillingProfileRepository profiles)
    : IRequestHandler<SaveFlatBillingProfileCommand>
{
    public async Task Handle(SaveFlatBillingProfileCommand request, CancellationToken cancellationToken = default)
    {
        var profile = request.Profile;
        var existing = await profiles.GetProfileAsync(profile.FlatId, cancellationToken);
        if (existing is null) existing = new FlatBillingProfile { FlatId = profile.FlatId };
        existing.Title = string.IsNullOrWhiteSpace(profile.Title) ? "Monthly Rent" : profile.Title; 
        existing.MonthlyAmount = profile.MonthlyAmount; 
        existing.DueDayOfMonth = profile.DueDayOfMonth <= 0 ? 1 : profile.DueDayOfMonth; 
        existing.IsActive = profile.IsActive;
        await profiles.SaveProfileAsync(existing, cancellationToken);
        if (!existing.IsActive) return;

        var today = DateTime.Today; 
        var assignment = await profiles.GetCurrentAssignmentAsync(profile.FlatId, today, cancellationToken); 
        if (assignment is null) return;

        var firstOfMonth = new DateTime(today.Year, today.Month, 1); 
        if (new DateTime(assignment.StartDate.Year, assignment.StartDate.Month, 1) > firstOfMonth) return;
        
        if (!await profiles.TenantBillExistsAsync(profile.FlatId, assignment.TenantUserId, firstOfMonth, cancellationToken)) 
            await profiles.AddTenantBillAsync(new TenantBill { FlatId = profile.FlatId, TenantUserId = assignment.TenantUserId, Title = string.IsNullOrWhiteSpace(existing.Title) ? "Monthly Rent" : existing.Title, BillDate = firstOfMonth, Amount = existing.MonthlyAmount }, cancellationToken);
    }
}
