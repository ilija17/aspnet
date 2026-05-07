using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class Casino
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string LicenseNumber { get; set; }
    public DateTime FoundedDate { get; set; }

    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
