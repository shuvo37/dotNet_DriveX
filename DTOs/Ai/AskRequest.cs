using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Ai;

public class AskRequest
{
    [Required(ErrorMessage = "Please type a question")]
    [StringLength(500, ErrorMessage = "Question is too long (max 500 characters)")]
    public string Question { get; set; } = string.Empty;
}
