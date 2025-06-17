using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Security.Cryptography;
using System.Text;
using NoteApi.Infrastructure.Database;
using NoteApi.Common.Models;

namespace NoteApi.Features.Auth;

public static class Register
{
    public record Command(string Email, string Password, string FullName);

    public record Response(int Id, string Email, string FullName);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(6);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(100);
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app) =>
            app.MapPost("/auth/register", Handle)
               .WithTags("Authentication")
               .WithSummary("Register a new user");

        static async Task<IResult> Handle(
            Command command,
            AppDbContext db,
            IValidator<Command> validator,
            Microsoft.Extensions.Logging.ILogger logger,
            CancellationToken ct)
        {
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var existingUser = await db.Users
                .FirstOrDefaultAsync(u => u.Email == command.Email, ct);

            if (existingUser != null)
            {
                return Results.Conflict(new { message = "Email already exists" });
            }

            var passwordHash = HashPassword(command.Password);
            var user = new User
            {
                Email = command.Email,
                PasswordHash = passwordHash,
                FullName = command.FullName
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("User registered successfully with email {Email}", command.Email);

            var response = new Response(user.Id, user.Email, user.FullName);
            return Results.Created($"/users/{user.Id}", response);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}