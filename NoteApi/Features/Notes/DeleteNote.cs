using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NoteApi.Infrastructure.Database;
using NoteApi.Common.Extensions;

namespace NoteApi.Features.Notes;

public static class DeleteNote
{
    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app) =>
            app.MapDelete("/notes/{id}", Handle)
               .RequireAuthorization()
               .WithTags("Notes")
               .WithSummary("Delete a note (soft delete)");

        static async Task<IResult> Handle(
            int id,
            ClaimsPrincipal user,
            AppDbContext db,
            Microsoft.Extensions.Logging.ILogger logger,
            CancellationToken ct)
        {
            var userId = user.GetUserId();

            var note = await db.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, ct);

            if (note is null)
            {
                logger.LogWarning("Note {NoteId} not found for user {UserId}", id, userId);
                return Results.NotFound();
            }

            note.IsDeleted = true;
            note.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Note {NoteId} deleted by user {UserId}", note.Id, userId);

            return Results.NoContent();
        }
    }
}