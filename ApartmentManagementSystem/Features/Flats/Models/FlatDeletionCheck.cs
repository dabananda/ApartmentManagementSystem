namespace ApartmentManagementSystem.Features.Flats.Models;

public sealed record FlatDeletionCheck(bool HasBills, bool HasTenants, bool HasActiveAssignments, bool HasEntryLogs)
{
    public bool HasRelatedRecords => HasBills || HasTenants || HasActiveAssignments || HasEntryLogs;
}
