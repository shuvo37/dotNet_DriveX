using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Ai;

// One earlier message of the conversation, kept by the browser and sent back with the next question.
public class ChatMessage
{
    // "user" for what the person typed, "model" for what the AI answered.
    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Text { get; set; } = string.Empty;
}
