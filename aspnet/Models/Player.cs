using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Models;

public class Player
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Ime je obavezno")]
    [StringLength(100, ErrorMessage = "Ime ne smije biti duže od 100 znakova")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Prezime je obavezno")]
    [StringLength(100, ErrorMessage = "Prezime ne smije biti duže od 100 znakova")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Email je obavezan")]
    [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
    [StringLength(200)]
    public string Email { get; set; }

    [Required(ErrorMessage = "Datum rođenja je obavezan")]
    public DateTime DateOfBirth { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Saldo mora biti 0 ili veći")]
    [Precision(18, 2)]
    public decimal Balance { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
