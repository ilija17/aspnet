using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ime je obavezno")]
    [StringLength(100, ErrorMessage = "Ime ne smije biti duže od 100 znakova")]
    [Display(Name = "Ime")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Prezime je obavezno")]
    [StringLength(100, ErrorMessage = "Prezime ne smije biti duže od 100 znakova")]
    [Display(Name = "Prezime")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Datum rođenja je obavezan")]
    [DataType(DataType.Date)]
    [Display(Name = "Datum rođenja")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Email je obavezan")]
    [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "OIB je obavezan")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "OIB mora imati točno 11 znamenki")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "OIB smije sadržavati samo brojeve")]
    [Display(Name = "OIB")]
    public string OIB { get; set; }

    [Required(ErrorMessage = "JMBG je obavezan")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "JMBG mora imati točno 13 znamenki")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "JMBG smije sadržavati samo brojeve")]
    [Display(Name = "JMBG")]
    public string JMBG { get; set; }

    [Required(ErrorMessage = "Lozinka je obavezna")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati barem 6 znakova")]
    [DataType(DataType.Password)]
    [Display(Name = "Lozinka")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Potvrda lozinke")]
    [Compare("Password", ErrorMessage = "Lozinke se ne podudaraju")]
    public string ConfirmPassword { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Email je obavezan")]
    [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Lozinka je obavezna")]
    [DataType(DataType.Password)]
    [Display(Name = "Lozinka")]
    public string Password { get; set; }

    [Display(Name = "Zapamti me")]
    public bool RememberMe { get; set; }
}

public class ExternalLoginViewModel
{
    [Required(ErrorMessage = "Ime je obavezno")]
    [StringLength(100, ErrorMessage = "Ime ne smije biti duže od 100 znakova")]
    [Display(Name = "Ime")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Prezime je obavezno")]
    [StringLength(100, ErrorMessage = "Prezime ne smije biti duže od 100 znakova")]
    [Display(Name = "Prezime")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Datum rođenja je obavezan")]
    [DataType(DataType.Date)]
    [Display(Name = "Datum rođenja")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Email je obavezan")]
    [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "OIB je obavezan")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "OIB mora imati točno 11 znamenki")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "OIB smije sadržavati samo brojeve")]
    [Display(Name = "OIB")]
    public string OIB { get; set; }

    [Required(ErrorMessage = "JMBG je obavezan")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "JMBG mora imati točno 13 znamenki")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "JMBG smije sadržavati samo brojeve")]
    [Display(Name = "JMBG")]
    public string JMBG { get; set; }

    // Moraju biti nullable: ne dolaze od korisnika nego iz hidden inputa,
    // a implicitni required za non-nullable stringove ruši ModelState
    public string? ProviderDisplayName { get; set; }
    public string? ReturnUrl { get; set; }
}

public class ProfileViewModel
{
    // Samo za prikaz — email je ujedno username pa se ne mijenja kroz profil
    public string? Email { get; set; }

    [Required(ErrorMessage = "Ime je obavezno")]
    [StringLength(100, ErrorMessage = "Ime ne smije biti duže od 100 znakova")]
    [Display(Name = "Ime")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Prezime je obavezno")]
    [StringLength(100, ErrorMessage = "Prezime ne smije biti duže od 100 znakova")]
    [Display(Name = "Prezime")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Datum rođenja je obavezan")]
    [DataType(DataType.Date)]
    [Display(Name = "Datum rođenja")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "OIB je obavezan")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "OIB mora imati točno 11 znamenki")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "OIB smije sadržavati samo brojeve")]
    [Display(Name = "OIB")]
    public string OIB { get; set; }

    [Required(ErrorMessage = "JMBG je obavezan")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "JMBG mora imati točno 13 znamenki")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "JMBG smije sadržavati samo brojeve")]
    [Display(Name = "JMBG")]
    public string JMBG { get; set; }
}
