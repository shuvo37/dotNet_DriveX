using DriveX.DTOs.Ai;

namespace DriveX.Services;

public interface IAiAssistantService
{
    // Answers a customer's question about the cars on DriveX.
    Task<string> AskAsync(string question, IEnumerable<ChatMessage>? history = null);
}
