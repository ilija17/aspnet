using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace aspnet.Models;

public class AppUser : IdentityUser
{
    [Required(ErrorMessage = "OIB je obavezan")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "OIB mora imati točno 11 znamenki")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "OIB smije sadržavati samo brojeve")]
    public string OIB { get; set; }

    [Required(ErrorMessage = "JMBG je obavezan")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "JMBG mora imati točno 13 znamenki")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "JMBG smije sadržavati samo brojeve")]
    public string JMBG { get; set; }
}
