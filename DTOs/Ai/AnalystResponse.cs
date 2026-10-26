namespace DriveX.DTOs.Ai;

// Answer = the analysis text. ToolsUsed = which data the AI looked at, so the admin can see where it came from.
public record AnalystResponse(string Answer, List<string> ToolsUsed);
