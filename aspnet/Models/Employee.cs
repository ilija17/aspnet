using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet.Models;

public class Employee
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Ime je obavezno")]
    [StringLength(100, ErrorMessage = "Ime ne smije biti duže od 100 znakova")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Prezime je obavezno")]
    [StringLength(100, ErrorMessage = "Prezime ne smije biti duže od 100 znakova")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Pozicija je obavezna")]
    [StringLength(100, ErrorMessage = "Pozicija ne smije biti duža od 100 znakova")]
    public string Position { get; set; }

    [Required(ErrorMessage = "Casino je obavezan")]
    [ForeignKey("Casino")]
    public int CasinoId { get; set; }
    public virtual Casino Casino { get; set; }
}
