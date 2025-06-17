using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Security.Claims;
using NoteApi.Infrastructure.Database;
using NoteApi.Common.Extensions;
using NoteApi.Common.Models;

namespace NoteApi.Features.Notes;

public static class UpdateNote
{
    public record Command(int Id, string Title, string Content, List<string> Tags);

    public record Response(
        int Id,
        string Title,
        string Content,
        List<string> Tags,
        DateTime UpdatedAt);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Content)
                .NotEmpty();

            RuleFor(x => x.Tags)
                .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag)))
                .WithMessage("Tags cannot be empty");
        }
    }

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app) =>
            app.MapPut("/notes/{id}", Handle)
               .RequireAuthorization()
               .WithTags("Notes")
               .WithSummary("Update an existing note");

        static async Task<IResult> Handle(
            int id,
            Command command,
            ClaimsPrincipal user,
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

            var userId = user.GetUserId();

            var note = await db.Notes
                .Include(n => n.NoteTags)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, ct);

            if (note is null)
            {
                logger.LogWarning("Note {NoteId} not found for user {UserId}", id, userId);
                return Results.NotFound();
            }

            note.Title = command.Title;
            note.Content = command.Content;
            note.UpdatedAt = DateTime.UtcNow;

            db.NoteTags.RemoveRange(note.NoteTags);

            var noteTags = new List<NoteTag>();
            foreach (var tagName in command.Tags.Distinct())
            {
                var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == tagName, ct);
                if (tag == null)
                {
                    tag = new Tag { Name = tagName };
                    db.Tags.Add(tag);
                    await db.SaveChangesAsync(ct);
                }

                noteTags.Add(new NoteTag { NoteId = note.Id, TagId = tag.Id });
            }

            db.NoteTags.AddRange(noteTags);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Note {NoteId} updated by user {UserId}", note.Id, userId);

            var response = new Response(
                note.Id,
                note.Title,
                note.Content,
                command.Tags,
                note.UpdatedAt);

            return Results.Ok(response);
        }
    }
}