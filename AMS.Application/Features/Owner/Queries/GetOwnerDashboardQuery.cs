using AMS.Application.Mediator;
using AMS.Application.Interfaces.Owner;
using AMS.Application.Features.Owner.DTOs;

namespace AMS.Application.Features.Owner.Queries;

public record GetOwnerDashboardQuery(string OwnerId) : IRequest<OwnerDashboardVM>;

public class GetOwnerDashboardQueryHandler(IOwnerRepository repository)
    : IRequestHandler<GetOwnerDashboardQuery, OwnerDashboardVM>
{
    public async Task<OwnerDashboardVM> Handle(GetOwnerDashboardQuery request, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var data = await repository.GetDashboardDataAsync(request.OwnerId, monthStart, cancellationToken);

        return new OwnerDashboardVM
        {
            FlatsOwnedCount = data.FlatsOwnedCount,
            FlatsOccupiedCount = data.FlatsOccupiedCount,
            RentTotalBilled = data.RentTotalBilled,
            RentTotalPaid = data.RentTotalPaid,
            RentPaidThisMonth = data.RentPaidThisMonth,
            CommonTotalBilled = data.CommonTotalBilled,
            CommonTotalPaid = data.CommonTotalPaid,
            Tenants = data.Tenants.ToList(),
            RecentRent = data.RecentRent.ToList(),
            RecentCommon = data.RecentCommon.ToList()
        };
    }
}
