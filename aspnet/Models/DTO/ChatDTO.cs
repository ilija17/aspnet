using System.ComponentModel.DataAnnotations;

namespace aspnet.Models.DTO;

public class ChatMessageDTO
{
    /// "user" ili "assistant"
    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
}

public class ChatRequestDTO
{
    [Required]
    [MinLength(1)]
    public List<ChatMessageDTO> Messages { get; set; } = [];
}

public class ChatReplyDTO
{
    public string Reply { get; set; } = string.Empty;
}
