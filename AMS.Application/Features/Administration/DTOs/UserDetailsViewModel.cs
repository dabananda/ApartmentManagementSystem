using AMS.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AMS.Application.Features.Administration.DTOs
{
    public class UserDetailsViewModel
    {
        public string Id { get; set; } = default!;

        [Display(Name = "Full Name")]
        public string Fullname { get; set; } = default!;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = default!;

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = default!;

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Account Locked Until")]
        public DateTimeOffset? LockoutEnd { get; set; }

        [Display(Name = "Failed Login Attempts")]
        public int AccessFailedCount { get; set; }

        [Display(Name = "User Roles")]
        public List<string> Roles { get; set; } = [];

        [Display(Name = "Building Name")]
        public string? BuildingName { get; set; }

        [Display(Name = "Building Address")]
        public string? BuildingAddress { get; set; }

        [Display(Name = "Flats Owned")]
        public int FlatCount { get; set; }

        [Display(Name = "Active Tenants")]
        public int TenantCount { get; set; }

        [Display(Name = "Outstanding Bills")]
        public int OutstandingBillsCount { get; set; }

        [Display(Name = "Outstanding Amount")]
        public decimal OutstandingAmount { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "Account Status")]
        public string AccountStatus { get; set; } = default!;

        // ─── Computed Properties (using Roles.* constants as SSOT) ────────────

        public string PrimaryRole => Roles.FirstOrDefault() ?? Domain.Constants.Roles.User;
        public bool HasMultipleRoles => Roles.Count > 1;
        public bool IsOwner => Roles.Contains(Domain.Constants.Roles.Owner);
        public bool IsStaff => Roles.Contains(Domain.Constants.Roles.Staff);
        public bool IsPresident => Roles.Contains(Domain.Constants.Roles.President);
        public bool HasOutstandingBills => OutstandingBillsCount > 0;

        public string StatusClass => AccountStatus switch
        {
            "Active" => "success",
            "Locked" => "danger",
            "Pending Verification" => "warning",
            _ => "secondary"
        };
    }
}
