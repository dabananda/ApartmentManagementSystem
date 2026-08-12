namespace ApartmentManagementSystem.Application.Features.Flats.DTOs;

public sealed record FlatDeletionCheck(bool HasBills, bool HasTenants, bool HasActiveAssignments, bool HasEntryLogs)
{
    public bool HasRelatedRecords => HasBills || HasTenants || HasActiveAssignments || HasEntryLogs;
}
