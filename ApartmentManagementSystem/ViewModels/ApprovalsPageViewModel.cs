using Microsoft.AspNetCore.Mvc.Rendering;

namespace ApartmentManagementSystem.ViewModels
{
    public class ApprovalsPageViewModel
    {
        public ApprovalsFilterViewModel Filter { get; set; } = new ApprovalsFilterViewModel();
        public List<SelectListItem> Buildings { get; set; } = new();
        public List<PendingUserItemViewModel> PendingUsers { get; set; } = new();
        public int Total { get; set; }
    }
}
