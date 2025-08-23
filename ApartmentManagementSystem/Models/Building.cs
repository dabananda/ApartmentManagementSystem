using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Models
{
    public class Building
    {
        public Guid Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [StringLength(255)]
        public string Address { get; set; }
        public ICollection<Flat>? Flats { get; set; }
        public ICollection<CommonExpense>? CommonExpenses { get; set; } = new List<CommonExpense>();
    }
}
