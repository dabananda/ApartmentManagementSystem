using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Flats.DTOs;
namespace AMS.Application.Features.Tenancy.Services;

public interface IFlatBillingProfileService { Task<IReadOnlyList<FlatProfileRow>> GetRowsAsync(string? ownerId); Task<Flat?> GetFlatAsync(Guid flatId); Task<FlatBillingProfile?> GetProfileAsync(Guid flatId); Task SaveAsync(FlatBillingProfile profile); }
