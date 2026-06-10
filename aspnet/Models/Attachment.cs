using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet.Models;

public class Attachment
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Casino je obavezan")]
    [ForeignKey("Casino")]
    public int CasinoId { get; set; }
    public virtual Casino Casino { get; set; }

    [Required]
    [StringLength(300)]
    public string FileName { get; set; }

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; }

    [StringLength(200)]
    public string ContentType { get; set; }

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }
}
