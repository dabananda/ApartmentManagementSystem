using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Features.Administration.DTOs;

public class ApprovalsPageViewModel
{
    public ApprovalsFilterViewModel Filter { get; set; } = new ApprovalsFilterViewModel();
    public List<SelectListItem> Buildings { get; set; } = new();
    public List<PendingUserItemViewModel> PendingUsers { get; set; } = new();
    public int Total { get; set; }
}
