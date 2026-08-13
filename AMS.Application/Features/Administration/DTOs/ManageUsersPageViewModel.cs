using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Features.Administration.DTOs;

public class ManageUsersPageViewModel
{
    public ManageUsersFilterViewModel Filter { get; set; } = new();
    public IEnumerable<SelectListItem> Buildings { get; set; } = [];
    public IEnumerable<UserRowViewModel> Users { get; set; } = [];
    public int Total { get; set; }
}


