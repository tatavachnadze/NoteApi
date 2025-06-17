using System.Security.Claims;
using NoteApi.Infrastructure.Services;

namespace NoteApi.Features.Auth;

public static class RefreshToken
{
    public record Command(string Token);

    public record Response(string Token);

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app) =>
            app.MapPost("/auth/refresh", Handle)
               .WithTags("Authentication")
               .WithSummary("Refresh JWT token");

        static async Task<IResult> Handle(
           Command command,
           IJwtService jwtService,
           Microsoft.Extensions.Logging.ILogger logger)
        {
            var principal = await jwtService.ValidateTokenAsync(command.Token);
            if (principal == null)
            {
                return Results.Unauthorized();
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return Results.Unauthorized();
            }

            var newToken = await jwtService.GenerateTokenAsync(int.Parse(userId), email);

            logger.LogInformation("Token refreshed for user {Email}", email);

            var response = new Response(newToken);
            return Results.Ok(response);
        }
    }
}