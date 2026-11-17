using System.Diagnostics;
using System.Text.Json;
using ReferralReview.Api.Inference;
using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal sealed class ReferralReviewService(
    IInferenceClient inference, ILogger<ReferralReviewService> logger) : IReferralReviewService
{
    public async Task<Review> ReviewAsync(Referral request, Profile profile, CancellationToken cancellation)
    {
        var reviewId = Guid.NewGuid().ToString("N");
        var clock = Stopwatch.StartNew();
        var sources = ReviewSources.Build(request);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(12));

        Review review;
        string? failure = null;
        try
        {
            var proposed = await inference.InferAsync(request.Profile, profile, sources, deadline.Token);
            var checks = profile.Checks.Keys.ToDictionary(key => key, key =>
            {
                var matching = proposed.Where(finding => finding?.Check == key).Take(2).ToArray();
                return EvidenceValidator.Validate(matching.Length == 1 ? matching[0] : null, sources);
            });
            var result = checks.Values.All(item => item.Status == "present")
                ? "no_gaps_found"
                : "review_suggested";
            review = new Review(reviewId, result, inference.Mode, checks);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            logger.LogInformation("Review {ReviewId} mode {Mode} cancelled after {ElapsedMs} ms",
                reviewId, inference.Mode, clock.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or OperationCanceledException)
        {
            failure = exception switch
            {
                HttpRequestException or IOException => "provider_http",
                JsonException => "invalid_model_response",
                _ => "timeout"
            };
            review = new Review(reviewId, "review_unavailable", inference.Mode, new());
        }

        logger.LogInformation(
            "Review {ReviewId} mode {Mode} outcome {Result} elapsed {ElapsedMs} ms error {ErrorCategory}",
            reviewId, inference.Mode, review.Result, clock.ElapsedMilliseconds, failure ?? "none");
        return review;
    }
}