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
        Referral request, ReviewProfiles profiles, IReferralReviewService service, CancellationToken cancellation)
    {
        var profile = profiles.Resolve(request.Profile, request.ReferralType);
        if (profile is null || !ReferralRequestValidator.IsValid(request))
            return Results.BadRequest(new { error = "Invalid draft, supporting sources or review profile." });

        return Results.Ok(await service.ReviewAsync(request, profile, cancellation));
    }
}