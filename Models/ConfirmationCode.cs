namespace DriveX.Models;

// Maps to the existing "conformation_code" table (the table name keeps its old spelling).
// Holds the email verification codes. Expiry logic will live in a service, not here.
public class ConfirmationCode
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Expiry moment as Unix time in milliseconds, same as the Java version stored it.
    public long ExpiresAt { get; set; }

    public bool? IsUsed { get; set; }
}
