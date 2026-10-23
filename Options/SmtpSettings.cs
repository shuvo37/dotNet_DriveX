namespace DriveX.Options;

// Bound from the "Smtp" section of configuration (user-secrets in development).
public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Shown as the sender. Falls back to Username when left empty.
    public string From { get; set; } = string.Empty;
}
