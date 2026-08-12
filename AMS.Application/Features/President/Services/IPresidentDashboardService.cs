using AMS.Application.Features.President.DTOs;

namespace AMS.Application.Features.President.Services;

public interface IPresidentDashboardService { Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default); }
