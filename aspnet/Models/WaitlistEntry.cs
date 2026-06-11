using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class WaitlistEntry
{
    [Key]
    public int Id { get; set; }

    [Required, EmailAddress, StringLength(320)]
    public string Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
