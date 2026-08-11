using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Flat;
namespace ApartmentManagementSystem.Features.Tenancy.Services;
public interface IFlatBillingProfileService { Task<IReadOnlyList<FlatProfileRow>> GetRowsAsync(string? ownerId); Task<Flat?> GetFlatAsync(Guid flatId); Task<FlatBillingProfile?> GetProfileAsync(Guid flatId); Task SaveAsync(FlatBillingProfile profile); }
