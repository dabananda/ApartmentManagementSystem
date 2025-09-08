namespace ApartmentManagementSystem.ViewModels
{
    public class BuildingIndexPageViewModel
    {
        public BuildingIndexFilterViewModel Filter { get; set; } = new();
        public List<BuildingListItemViewModel> Buildings { get; set; } = new();
        public int Total { get; set; }
    }
}
