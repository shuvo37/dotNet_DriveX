namespace DriveX.DTOs.Auth;

// What we take from the Google account.
public record GoogleProfile(string Email, string FirstName, string LastName, string? Picture);
