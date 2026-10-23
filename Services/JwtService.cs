using System.Text;
using DriveX.Enums;
using DriveX.Models;
using DriveX.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DriveX.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            // Same claim names as the Spring token. The React code reads
            // "role", "sub" (email), "userId" and "exp" straight from the token.
            Claims = new Dictionary<string, object>
            {
                ["sub"] = user.Email,
                ["userId"] = user.UserId,
                ["role"] = (user.Role ?? Role.USER).ToString()
            },
            IssuedAt = now,
            Expires = now.AddMilliseconds(_settings.ExpirationMs),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
