using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet.Models;

public class Employee
{
    [Key]
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Position { get; set; }

    [ForeignKey("Casino")]
    public int CasinoId { get; set; }
    public virtual Casino Casino { get; set; }
}
