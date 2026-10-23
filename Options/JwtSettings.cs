namespace DriveX.Options;

// Bound from the "Jwt" section of configuration.
// Secret comes from user-secrets; ExpirationMs lives in appsettings.json.
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public long ExpirationMs { get; set; } = 86_400_000; // 24 hours, same as Spring
}
