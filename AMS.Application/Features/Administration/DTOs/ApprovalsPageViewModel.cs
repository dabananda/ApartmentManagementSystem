using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Features.Administration.DTOs;

public class ApprovalsPageViewModel
{
    public ApprovalsFilterViewModel Filter { get; set; } = new ApprovalsFilterViewModel();
    public IEnumerable<SelectListItem> Buildings { get; set; } = [];
    public IEnumerable<PendingUserItemViewModel> PendingUsers { get; set; } = [];
    public int Total { get; set; }
}


