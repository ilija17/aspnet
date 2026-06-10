using System.ComponentModel.DataAnnotations;
using aspnet.Models;

namespace aspnet.Models.DTO;

public class CasinoInputDTO
{
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
}

public class GameInputDTO
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Naziv igre je obavezan")]
    [StringLength(100, ErrorMessage = "Naziv ne smije biti duži od 100 znakova")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Tip igre je obavezan")]
    public GameType Type { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Minimalni ulog mora biti između 0.01 i 1,000,000")]
    public decimal MinBet { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Maksimalni ulog mora biti između 0.01 i 1,000,000")]
    public decimal MaxBet { get; set; }

    [StringLength(500, ErrorMessage = "Opis ne smije biti duži od 500 znakova")]
    public string? Description { get; set; }
}

public class PlayerInputDTO
{
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
    public decimal Balance { get; set; }
}

public class EmployeeInputDTO
{
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

    [Range(1, int.MaxValue, ErrorMessage = "Casino je obavezan")]
    public int CasinoId { get; set; }
}

public class TableInputDTO
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Broj stola mora biti pozitivan")]
    public int TableNumber { get; set; }

    public bool IsAvailable { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Minimalni ulog mora biti između 0.01 i 1,000,000")]
    public decimal MinBet { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Maksimalni ulog mora biti između 0.01 i 1,000,000")]
    public decimal MaxBet { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Casino je obavezan")]
    public int CasinoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Igra je obavezna")]
    public int GameId { get; set; }
}

public class TransactionInputDTO
{
    public int Id { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Iznos mora biti između 0.01 i 1,000,000")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Tip transakcije je obavezan")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "Datum transakcije je obavezan")]
    public DateTime CreatedAt { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Igrač je obavezan")]
    public int PlayerId { get; set; }
}

public class ReservationInputDTO
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Datum rezervacije je obavezan")]
    public DateTime ReservedAt { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Igrač je obavezan")]
    public int PlayerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Stol je obavezan")]
    public int TableId { get; set; }
}
