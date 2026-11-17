using System.Diagnostics;
using System.Text.Json;
using ReferralReview.Api.Inference;
using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal sealed class ReferralReviewService(
    IChatCompletionsClient inference,
    ILogger<ReferralReviewService> logger
) : IReferralReviewService
{
    public async Task<ReviewResponse> ReviewAsync(
        ReviewRequest request,
        ReviewProfile profile,
        CancellationToken cancellation
    )
    {
        const string mode = "gateway";
        var reviewId = Guid.NewGuid().ToString("N");
        var clock = Stopwatch.StartNew();
        var sources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["draft"] = request.Draft,
        };
        foreach (var source in request.SupportingSources)
            sources.Add(source.Id, source.Text);

        var input = new GatewayReviewInput(request.Profile, profile.Checks, sources);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(12));

        ReviewResponse review;
        string? failure = null;
        try
        {
            var proposed = await inference.InferAsync(input, deadline.Token);
            var checks = profile.Checks.Keys.ToDictionary(
                key => key,
                key =>
                {
                    var matching = proposed
                        .Where(finding => finding?.Check == key)
                        .Take(2)
                        .ToArray();
                    return EvidenceValidator.Validate(
                        matching.Length == 1 ? matching[0] : null,
                        sources
                    );
                }
            );
            var result = checks.Values.All(item => item.Status == "present")
                ? "no_gaps_found"
                : "review_suggested";
            review = new ReviewResponse(reviewId, result, mode, checks);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            logger.LogInformation(
                "Review {ReviewId} mode {Mode} cancelled after {ElapsedMs} ms",
                reviewId,
                mode,
                clock.ElapsedMilliseconds
            );
            throw;
        }
        catch (Exception exception)
            when (exception
                    is HttpRequestException
                        or IOException
                        or JsonException
                        or OperationCanceledException
            )
        {
            failure = exception switch
            {
                HttpRequestException { StatusCode: { } status } => $"provider_http_{(int)status}",
                HttpRequestException or IOException => "provider_http",
                JsonException => "invalid_model_response",
                _ => "timeout",
            };
            review = new ReviewResponse(reviewId, "review_unavailable", mode, new());
        }

        logger.LogInformation(
            "Review {ReviewId} mode {Mode} outcome {Result} elapsed {ElapsedMs} ms error {ErrorCategory}",
            reviewId,
            mode,
            review.Result,
            clock.ElapsedMilliseconds,
            failure ?? "none"
        );
        return review;
    }
}
