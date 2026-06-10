using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class Casino
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Naziv kasina je obavezan")]
    [StringLength(200, ErrorMessage = "Naziv ne smije biti duži od 200 znakova")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Adresa je obavezna")]
    [StringLength(300, ErrorMessage = "Adresa ne smije biti duža od 300 znakova")]
    public string Address { get; set; }

    [Required(ErrorMessage = "Broj licence je obavezan")]
    [StringLength(50, ErrorMessage = "Broj licence ne smije biti duži od 50 znakova")]
    public string LicenseNumber { get; set; }

    [Required(ErrorMessage = "Datum osnivanja je obavezan")]
    public DateTime FoundedDate { get; set; }

    public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
