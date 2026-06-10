using TodoApi.Models;

namespace TodoApi.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
