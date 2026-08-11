using Microsoft.AspNetCore.Mvc.Rendering;

namespace ApartmentManagementSystem.Features.Administration.ViewModels
{
    public class ManageUsersPageViewModel
    {
        public ManageUsersFilterViewModel Filter { get; set; } = new();
        public List<SelectListItem> Buildings { get; set; } = new();
        public List<UserRowViewModel> Users { get; set; } = new();
        public int Total { get; set; }
    }
}
