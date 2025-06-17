using System.Security.Claims;

namespace NoteApi.Infrastructure.Services;

public interface IJwtService
{
    Task<string> GenerateTokenAsync(int userId, string email);
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
}
