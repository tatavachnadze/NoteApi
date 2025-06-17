using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NoteApi.Infrastructure.Database;
using NoteApi.Common.Extensions;

namespace NoteApi.Features.Notes;

public static class GetNoteById
{
    public record Query(int Id, int UserId);

    public record Response(
        int Id,
        string Title,
        string Content,
        List<string> Tags,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app) =>
            app.MapGet("/notes/{id}", Handle)
               .RequireAuthorization()
               .WithTags("Notes")
               .WithSummary("Get note by ID");

        static async Task<IResult> Handle(
            int id,
            ClaimsPrincipal user,
            AppDbContext db,
            Microsoft.Extensions.Logging.ILogger logger,
            CancellationToken ct)
        {
            var userId = user.GetUserId();

            var note = await db.Notes
                .Include(n => n.NoteTags)
                .ThenInclude(nt => nt.Tag)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, ct);

            if (note is null)
            {
                logger.LogWarning("Note {NoteId} not found for user {UserId}", id, userId);
                return Results.NotFound();
            }

            var response = new Response(
                note.Id,
                note.Title,
                note.Content,
                note.NoteTags.Select(nt => nt.Tag.Name).ToList(),
                note.CreatedAt,
                note.UpdatedAt);

            return Results.Ok(response);
        }
    }
}