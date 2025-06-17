using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Security.Cryptography;
using System.Text;
using NoteApi.Infrastructure.Database;
using NoteApi.Infrastructure.Services;

namespace NoteApi.Features.Auth;

public static class Login
{
    public record Command(string Email, string Password);

    public record Response(string Token, int UserId, string Email, string FullName);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app) =>
            app.MapPost("/auth/login", Handle)
               .WithTags("Authentication")
               .WithSummary("Login with email and password");

        static async Task<IResult> Handle(
            Command command,
            AppDbContext db,
            IJwtService jwtService,
            IValidator<Command> validator,
            Microsoft.Extensions.Logging.ILogger logger,
            CancellationToken ct)
        {
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == command.Email, ct);

            if (user == null || !VerifyPassword(command.Password, user.PasswordHash))
            {
                logger.LogWarning("Failed login attempt for email {Email}", command.Email);
                return Results.Unauthorized();
            }

            var token = await jwtService.GenerateTokenAsync(user.Id, user.Email);

            logger.LogInformation("User {Email} logged in successfully", command.Email);

            var response = new Response(token, user.Id, user.Email, user.FullName);
            return Results.Ok(response);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var passwordHash = Convert.ToBase64String(hashedBytes);
            return passwordHash == hash;
        }
    }
}