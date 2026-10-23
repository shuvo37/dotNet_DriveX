using DriveX.Models;

namespace DriveX.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
