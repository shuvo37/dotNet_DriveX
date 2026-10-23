using System.Security.Cryptography;
using System.Text;
using DriveX.Data;
using DriveX.DTOs.PasswordReset;
using DriveX.Exceptions;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DriveX.Services;

// The forgot-password flow: send a 4-digit code, verify it, then set a new password.
public class PasswordResetService : IPasswordResetService
{
    private const long CodeValidityMillis = 3 * 60 * 1000;   // a code works for 3 minutes
    private const long BlockWindowMillis = 20 * 60 * 1000;   // at most 5 codes per 20 minutes
    private const long ResetWindowMillis = 10 * 60 * 1000;   // a verified code can reset the password for 10 minutes
    private const int MaxCodesPerWindow = 5;
    private const int MaxVerifyAttempts = 5;                 // wrong guesses allowed per code
    private const int BcryptWorkFactor = 10;

    private readonly DriveXDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        DriveXDbContext db,
        IEmailService emailService,
        IMemoryCache cache,
        ILogger<PasswordResetService> logger)
    {
        _db = db;
        _emailService = emailService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ForgotPasswordResponse> CheckEmailAsync(string email)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            return new ForgotPasswordResponse(false, false, null, null);
        }

        // Google-only accounts have no password to reset.
        if (string.IsNullOrEmpty(user.Password))
        {
            return new ForgotPasswordResponse(true, false, null, null);
        }

        var blockedUntil = await GetBlockedUntilAsync(user.Email);
        if (blockedUntil is not null)
        {
            return new ForgotPasswordResponse(true, true, null, blockedUntil);
        }

        var expiresAt = await IssueCodeAsync(user);
        return new ForgotPasswordResponse(true, true, expiresAt, null);
    }

    public async Task<bool> VerifyCodeAsync(string email, string code)
    {
        if (!await _db.Users.AnyAsync(u => u.Email == email))
        {
            throw new AppException("User not found");
        }

        var latest = await _db.ConfirmationCodes
            .Where(c => c.Email == email && c.IsUsed == false)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new AppException("Confirmation code doesn't exist");

        if (latest.ExpiresAt < NowMillis())
        {
            throw new AppException("Confirmation code has expired");
        }

        // A 4-digit code has only 10,000 possibilities, so wrong guesses are limited per code.
        var attemptsKey = AttemptsKey(email);
        var attempts = _cache.TryGetValue(attemptsKey, out int stored) ? stored : 0;

        if (attempts >= MaxVerifyAttempts)
        {
            throw new AppException("Too many wrong attempts. Please request a new code.");
        }

        if (!CodesMatch(latest.Code, code))
        {
            _cache.Set(attemptsKey, attempts + 1, TimeSpan.FromMilliseconds(BlockWindowMillis));
            return false;
        }

        latest.IsUsed = true; // "used" means verified: this code may now be exchanged for a new password
        await _db.SaveChangesAsync();
        _cache.Remove(attemptsKey);
        return true;
    }

    public async Task<ResendCodeResponse> ResendCodeAsync(string email)
    {
        var user = await GetResettableUserAsync(email);

        var blockedUntil = await GetBlockedUntilAsync(user.Email);
        if (blockedUntil is not null)
        {
            return new ResendCodeResponse(null, blockedUntil);
        }

        var expiresAt = await IssueCodeAsync(user);
        return new ResendCodeResponse(expiresAt, null);
    }

    public async Task ResetPasswordAsync(string email, string code, string newPassword)
    {
        var user = await GetResettableUserAsync(email);

        var verified = await _db.ConfirmationCodes
            .Where(c => c.Email == email && c.IsUsed == true)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new AppException("Confirmation code not found");

        if (!CodesMatch(verified.Code, code))
        {
            throw new AppException("Code mismatch");
        }

        // Spring never checked this, so a verified code stayed usable forever.
        if (NowMillis() - ToMillis(verified.CreatedAt) > ResetWindowMillis)
        {
            throw new AppException("Your verification has expired. Please request a new code.");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor);
        await _db.SaveChangesAsync();

        // Single use: remove this email's codes so the same code can never reset the password again.
        await _db.ConfirmationCodes.Where(c => c.Email == email).ExecuteDeleteAsync();
    }

    // A user who exists and has a password (Google-only accounts are refused).
    private async Task<User> GetResettableUserAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new AppException("User not found");

        if (string.IsNullOrEmpty(user.Password))
        {
            throw new AppException("This account uses Google sign-in and has no password to reset");
        }

        return user;
    }

    // Returns when the user may request codes again, or null if they are not blocked.
    private async Task<long?> GetBlockedUntilAsync(string email)
    {
        var recent = await _db.ConfirmationCodes.AsNoTracking()
            .Where(c => c.Email == email && c.IsUsed == false)
            .OrderByDescending(c => c.CreatedAt)
            .Take(MaxCodesPerWindow)
            .ToListAsync();

        if (recent.Count < MaxCodesPerWindow)
        {
            return null;
        }

        // The block ends when the oldest of the 5 codes leaves the 20-minute window.
        var unblockAt = ToMillis(recent[^1].CreatedAt) + BlockWindowMillis;
        return unblockAt > NowMillis() ? unblockAt : null;
    }

    // Creates the code, saves it and emails it. Returns when the code expires.
    private async Task<long> IssueCodeAsync(User user)
    {
        var code = RandomNumberGenerator.GetInt32(10_000).ToString("D4");
        var createdAt = DateTime.Now; // local time, like LocalDateTime.now() in Java
        var expiresAt = ToMillis(createdAt) + CodeValidityMillis;

        var entity = new ConfirmationCode
        {
            Email = user.Email,
            Code = code,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        _db.ConfirmationCodes.Add(entity);
        await _db.SaveChangesAsync();
        _cache.Remove(AttemptsKey(user.Email)); // a fresh code starts with a fresh attempt counter

        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Confirmation code from DriveX",
                $"Hi {user.FirstName} {user.LastName}, your DriveX confirmation code is: {code}\n\n" +
                "It expires in 3 minutes. If you didn't ask for it, you can ignore this email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send the confirmation code email");

            // Remove the unsent code so it doesn't count toward the 5-code limit.
            _db.ConfirmationCodes.Remove(entity);
            await _db.SaveChangesAsync();

            throw new AppException("We couldn't send the email. Please try again later.");
        }

        return expiresAt;
    }

    private static string AttemptsKey(string email) => $"reset-attempts:{email.ToLowerInvariant()}";

    private static long NowMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static long ToMillis(DateTime localTime) => new DateTimeOffset(localTime).ToUnixTimeMilliseconds();

    // Compares in constant time so response timing doesn't hint at correct digits.
    private static bool CodesMatch(string expected, string provided) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
}
