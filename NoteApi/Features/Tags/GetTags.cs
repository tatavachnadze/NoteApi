using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NoteApi.Infrastructure.Database;
using NoteApi.Common.Extensions;

namespace NoteApi.Features.Tags;

public static class GetTags
{
    public record Response(List<string> Tags);

    public static class Endpoint
    {
        public static void Map(IEndpointRouteBuilder app) =>
            app.MapGet("/tags", Handle)
               .RequireAuthorization()
               .WithTags("Tags")
               .WithSummary("Get all tags used by the current user");

        static async Task<IResult> Handle(
            ClaimsPrincipal user,
            AppDbContext db,
            Microsoft.Extensions.Logging.ILogger logger,
            CancellationToken ct)
        {
            var userId = user.GetUserId();

            var tags = await db.NoteTags
                .Where(nt => nt.Note.UserId == userId && !nt.Note.IsDeleted)
                .Select(nt => nt.Tag.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync(ct);

            logger.LogInformation("Retrieved {Count} tags for user {UserId}", tags.Count, userId);

            var response = new Response(tags);
            return Results.Ok(response);
        }
    }
}