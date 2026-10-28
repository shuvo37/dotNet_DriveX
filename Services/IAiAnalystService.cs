using DriveX.DTOs.Ai;

namespace DriveX.Services;

public interface IAiAnalystService
{
    // Lets the AI read the live DriveX data through read-only tools and analyse it for the admin.
    Task<AnalystResponse> AskAsync(string question, IEnumerable<ChatMessage>? history = null);
}
