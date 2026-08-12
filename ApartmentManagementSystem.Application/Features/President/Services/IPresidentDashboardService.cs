using ApartmentManagementSystem.Application.Features.President.DTOs;

namespace ApartmentManagementSystem.Application.Features.President.Services;

public interface IPresidentDashboardService { Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default); }
