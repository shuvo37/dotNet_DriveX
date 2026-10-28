using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Ai;

public class AskRequest
{
    [Required(ErrorMessage = "Please type a question")]
    [StringLength(500, ErrorMessage = "Question is too long (max 500 characters)")]
    public string Question { get; set; } = string.Empty;

    // Earlier messages of this conversation, oldest first. Optional: without it every question stands alone.
    [MaxLength(20, ErrorMessage = "Too many earlier messages")]
    public List<ChatMessage>? History { get; set; }
}
