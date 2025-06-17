using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Security.Claims;
using NoteApi.Infrastructure.Database;
using NoteApi.Common.Extensions;
using NoteApi.Common.Models;

namespace NoteApi.Features.Notes;

public static class CreateNote
{
    public record Command(string Title, string Content, List<string> Tags);

    public record Response(
        int Id,
        string Title,
        string Content,
        List<string> Tags,
        DateTime CreatedAt);

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
            app.MapPost("/notes", Handle)
               .RequireAuthorization()
               .WithTags("Notes")
               .WithSummary("Create a new note");

        static async Task<IResult> Handle(
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

            var note = new Note
            {
                UserId = userId,
                Title = command.Title,
                Content = command.Content
            };

            db.Notes.Add(note);
            await db.SaveChangesAsync(ct);
           
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

            logger.LogInformation("Note {NoteId} created by user {UserId}", note.Id, userId);

            var response = new Response(
                note.Id,
                note.Title,
                note.Content,
                command.Tags,
                note.CreatedAt);

            return Results.Created($"/notes/{note.Id}", response);
        }
    }
}