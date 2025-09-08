using System;

namespace ApartmentManagementSystem.ViewModels
{
    public class ManageUsersFilterViewModel
    {
        public Guid? BuildingId { get; set; }
        public string? Query { get; set; } // name/email/phone
        public string Role { get; set; } = "All"; // All | President | Owner | Tenant
        public bool LockedOnly { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
