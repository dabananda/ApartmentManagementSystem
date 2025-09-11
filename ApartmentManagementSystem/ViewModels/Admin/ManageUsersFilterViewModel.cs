using System;

namespace ApartmentManagementSystem.ViewModels.Admin
{
    public class ManageUsersFilterViewModel
    {
        public Guid? BuildingId { get; set; }
        public string? Query { get; set; }
        public string Role { get; set; } = "All";
        public bool LockedOnly { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
