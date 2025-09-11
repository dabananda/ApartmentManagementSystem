namespace ApartmentManagementSystem.ViewModels.Building
{
    public class BuildingListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Code { get; set; } = default!;
        public string? Address { get; set; }

        public int FlatsCount { get; set; }
        public int TenantsCount { get; set; }
        public int OwnersCount { get; set; }
        public string PresidentName { get; set; } = "-";
    }
}
