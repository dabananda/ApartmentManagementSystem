using AMS.Application.Mediator;
using AMS.Domain.Entities;
using AMS.Application.Features.Flats.DTOs;
using AMS.Application.Interfaces.Flats;
using AMS.Application.Interfaces.Buildings;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Application.Features.Flats.Queries
{
    public record GetAllFlatsWithReferencesQuery : IRequest<IReadOnlyList<Flat>>;
    public class GetAllFlatsWithReferencesQueryHandler(IFlatRepository flats) : IRequestHandler<GetAllFlatsWithReferencesQuery, IReadOnlyList<Flat>>
    {
        public Task<IReadOnlyList<Flat>> Handle(GetAllFlatsWithReferencesQuery request, CancellationToken cancellationToken) =>
            flats.GetAllWithReferencesAsync(cancellationToken);
    }

    public record GetActiveAssignmentsQuery : IRequest<IReadOnlyList<TenantAssignment>>;
    public class GetActiveAssignmentsQueryHandler(IFlatRepository flats) : IRequestHandler<GetActiveAssignmentsQuery, IReadOnlyList<TenantAssignment>>
    {
        public Task<IReadOnlyList<TenantAssignment>> Handle(GetActiveAssignmentsQuery request, CancellationToken cancellationToken) =>
            flats.GetActiveAssignmentsAsync(cancellationToken);
    }

    public record GetBuildingForFlatQuery(Guid BuildingId) : IRequest<Building?>;
    public class GetBuildingForFlatQueryHandler(IBuildingRepository buildings) : IRequestHandler<GetBuildingForFlatQuery, Building?>
    {
        public Task<Building?> Handle(GetBuildingForFlatQuery request, CancellationToken cancellationToken) =>
            buildings.GetAsync(request.BuildingId, false, cancellationToken);
    }

    public record GetFlatsForBuildingQuery(Guid BuildingId) : IRequest<IReadOnlyList<Flat>>;
    public class GetFlatsForBuildingQueryHandler(IFlatRepository flats) : IRequestHandler<GetFlatsForBuildingQuery, IReadOnlyList<Flat>>
    {
        public Task<IReadOnlyList<Flat>> Handle(GetFlatsForBuildingQuery request, CancellationToken cancellationToken) =>
            flats.GetForBuildingAsync(request.BuildingId, cancellationToken);
    }

    public record GetFlatByIdQuery(Guid FlatId, bool IncludeReferences = false, bool AsNoTracking = false) : IRequest<Flat?>;
    public class GetFlatByIdQueryHandler(IFlatRepository flats) : IRequestHandler<GetFlatByIdQuery, Flat?>
    {
        public Task<Flat?> Handle(GetFlatByIdQuery request, CancellationToken cancellationToken) =>
            flats.GetAsync(request.FlatId, request.IncludeReferences, request.AsNoTracking, cancellationToken);
    }

    public record GetFlatDeletionCheckQuery(Guid FlatId) : IRequest<FlatDeletionCheck>;
    public class GetFlatDeletionCheckQueryHandler(IFlatRepository flats) : IRequestHandler<GetFlatDeletionCheckQuery, FlatDeletionCheck>
    {
        public Task<FlatDeletionCheck> Handle(GetFlatDeletionCheckQuery request, CancellationToken cancellationToken) =>
            flats.GetDeletionCheckAsync(request.FlatId, cancellationToken);
    }
}
