using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NoteApi.Infrastructure.Database;
using NoteApi.Common.Extensions;

namespace NoteApi.Features.Notes;

public static class GetNotes
{
    public record Query(
        int Page = 1,
        int PageSize = 10,
        string? Search = null,
        string? Tags = null);

    public record Response(
        List<NoteItem> Notes,
        int TotalCount,
        int Page,
        int PageSize);

    public record NoteItem(
        int Id,
        string Title,
        string Content,
        List<string> Tags,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app) =>
            app.MapGet("/notes", Handle)
               .RequireAuthorization()
               .WithTags("Notes")
               .WithSummary("Get all notes with pagination and filtering");

        static async Task<IResult> Handle(
            int page,
            int pageSize,
            string? search,
            string? tags,
            ClaimsPrincipal user,
            AppDbContext db,
            Microsoft.Extensions.Logging.ILogger logger,
            CancellationToken ct)
        {
            var userId = user.GetUserId();
            var query = new Query(page, pageSize, search, tags);

            var notesQuery = db.Notes
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .Include(n => n.NoteTags)
                .ThenInclude(nt => nt.Tag)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.Search))
            {
                notesQuery = notesQuery.Where(n =>
                    n.Title.Contains(query.Search) ||
                    n.Content.Contains(query.Search));
            }

            if (!string.IsNullOrEmpty(query.Tags))
            {
                var tagNames = query.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
                notesQuery = notesQuery.Where(n =>
                    n.NoteTags.Any(nt => tagNames.Contains(nt.Tag.Name)));
            }

            var totalCount = await notesQuery.CountAsync(ct);

            var notes = await notesQuery
                .OrderByDescending(n => n.UpdatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(n => new NoteItem(
                    n.Id,
                    n.Title,
                    n.Content,
                    n.NoteTags.Select(nt => nt.Tag.Name).ToList(),
                    n.CreatedAt,
                    n.UpdatedAt))
                .ToListAsync(ct);

            logger.LogInformation("Retrieved {Count} notes for user {UserId}", notes.Count, userId);

            var response = new Response(notes, totalCount, query.Page, query.PageSize);
            return Results.Ok(response);
        }
    }
}