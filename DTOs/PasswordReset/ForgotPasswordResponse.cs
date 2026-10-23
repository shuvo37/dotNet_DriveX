namespace DriveX.DTOs.PasswordReset;

// "BlockUntill" keeps the old spelling on purpose: ForgotPassword.jsx reads exactly that name.
public record ForgotPasswordResponse(bool Exists, bool HasPassword, long? ExpiresAt, long? BlockUntill);
