using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Flats.DTOs;
namespace ApartmentManagementSystem.Application.Features.Tenancy.Services;

public interface IFlatBillingProfileService { Task<IReadOnlyList<FlatProfileRow>> GetRowsAsync(string? ownerId); Task<Flat?> GetFlatAsync(Guid flatId); Task<FlatBillingProfile?> GetProfileAsync(Guid flatId); Task SaveAsync(FlatBillingProfile profile); }
