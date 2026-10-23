namespace DriveX.DTOs.PasswordReset;

public record ResendCodeResponse(long? ExpiresAt, long? BlockedUntil);
