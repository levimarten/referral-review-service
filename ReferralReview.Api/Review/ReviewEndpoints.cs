using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal static class ReviewEndpoints
{
    public const string InferenceLimit = "review-concurrency";

    public static void MapReviewEndpoints(this WebApplication app)
    {
        app.MapPost("/review", HandleReviewAsync).RequireRateLimiting(InferenceLimit);
    }

    private static async Task<IResult> HandleReviewAsync(
        ReviewRequest request,
        ReviewProfiles profiles,
        IReferralReviewService service,
        CancellationToken cancellation
    )
    {
        var profile = profiles.Resolve(request.Profile, request.ReferralType);
        if (profile is null || !IsValidRequest(request))
            return Results.BadRequest(
                new { error = "Invalid draft, supporting sources or review profile." }
            );

        return Results.Ok(await service.ReviewAsync(request, profile, cancellation));
    }

    internal static bool IsValidRequest(ReviewRequest request)
    {
        if (
            string.IsNullOrWhiteSpace(request.Draft)
            || request.Draft.Length > 12_000
            || request.SupportingSources is not { Length: <= 5 }
        )
            return false;

        var ids = new HashSet<string>(StringComparer.Ordinal) { "draft" };
        return request.SupportingSources.All(source =>
            source is not null
            && source.Id is { Length: > 0 and <= 80 }
            && source.Id.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_'
            )
            && source.Text is { Length: <= 4_000 }
            && !string.IsNullOrWhiteSpace(source.Text)
            && ids.Add(source.Id)
        );
    }
}
