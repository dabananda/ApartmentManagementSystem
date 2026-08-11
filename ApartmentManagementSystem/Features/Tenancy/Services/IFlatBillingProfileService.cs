using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Flats.ViewModels;
namespace ApartmentManagementSystem.Features.Tenancy.Services;
public interface IFlatBillingProfileService { Task<IReadOnlyList<FlatProfileRow>> GetRowsAsync(string? ownerId); Task<Flat?> GetFlatAsync(Guid flatId); Task<FlatBillingProfile?> GetProfileAsync(Guid flatId); Task SaveAsync(FlatBillingProfile profile); }
