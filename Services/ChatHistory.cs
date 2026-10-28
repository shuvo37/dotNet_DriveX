using DriveX.DTOs.Ai;

namespace DriveX.Services;

// The history comes from the browser, so it is never trusted as is: only the last few messages are used,
// each is cut to a maximum length, and only the roles "user" and "model" are accepted.
public static class ChatHistory
{
    public static List<(string Role, string Text)> Clean(IEnumerable<ChatMessage>? history, int maxMessages, int maxChars)
    {
        var result = new List<(string Role, string Text)>();

        if (history is null)
        {
            return result;
        }

        foreach (var message in history.TakeLast(maxMessages))
        {
            var role = message.Role?.Trim().ToLowerInvariant();
            var text = message.Text?.Trim();

            if ((role != "user" && role != "model") || string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (text.Length > maxChars)
            {
                text = text[..maxChars];
            }

            result.Add((role, text));
        }

        // Gemini expects the conversation to start with the person's turn.
        while (result.Count > 0 && result[0].Role != "user")
        {
            result.RemoveAt(0);
        }

        return result;
    }
}
