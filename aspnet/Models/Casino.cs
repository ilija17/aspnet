namespace aspnet.Models;

public class Casino
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string LicenseNumber { get; set; }
    public DateTime FoundedDate { get; set; }

    public List<Table> Tables { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
}
