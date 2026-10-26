namespace DriveX.Options;

// Bound from the "Gemini" section of configuration. ApiKey comes from user-secrets, never from appsettings.json.
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    // A free-tier model. Change it here (or in user-secrets) if Google renames or retires it.
    public string Model { get; set; } = "gemini-3.1-flash-lite";

    // Used when the main model is overloaded or unavailable. Must be another model that your key can use.
    // Free-tier limits are per model, so a second model also helps when the first one hits its limit.
    public string FallbackModel { get; set; } = "gemini-3-flash-preview";
}
