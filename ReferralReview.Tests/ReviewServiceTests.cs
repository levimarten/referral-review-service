using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ReferralReview.Api.Inference;
using ReferralReview.Api.Reviewing;
using ReferralReview.Contracts;

namespace ReferralReview.Tests;

[TestClass]
public sealed class ReviewServiceTests
{
    private const string DraftText = "No current medication.";
    private static readonly ReviewRequest Request = new(
        "cardiology",
        "specialist_consultation",
        DraftText,
        []
    );
    private static readonly ReviewProfile Profile = new(
        Request.ReferralType,
        new() { ["medication"] = "State current medication." }
    );

    [TestMethod]
    public async Task SupportedFindingReturnsNoGaps()
    {
        // Given
        using var gateway = GatewayReturns(
            new Finding("medication", "present", "draft", DraftText)
        );

        // When
        var review = await ReviewAsync(gateway);

        // Then
        Assert.AreEqual("no_gaps_found", review.Result);
        Assert.AreEqual("present", review.Checks["medication"].Status);
    }

    [TestMethod]
    public async Task DuplicateFindingsCannotBeVerified()
    {
        // Given
        var finding = new Finding("medication", "present", "draft", DraftText);
        using var gateway = GatewayReturns(finding, finding);

        // When
        var review = await ReviewAsync(gateway);

        // Then
        Assert.AreEqual("review_suggested", review.Result);
        Assert.AreEqual("unable_to_verify", review.Checks["medication"].Status);
    }

    [TestMethod]
    public async Task MissingFindingCannotBeVerified()
    {
        // Given
        using var gateway = GatewayReturns();

        // When
        var review = await ReviewAsync(gateway);

        // Then
        Assert.AreEqual("review_suggested", review.Result);
        Assert.AreEqual("unable_to_verify", review.Checks["medication"].Status);
    }

    [TestMethod]
    public async Task GatewayFailureReturnsReviewUnavailable()
    {
        // Given
        using var gateway = new HttpClient(
            new GatewayHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
        );

        // When
        var review = await ReviewAsync(gateway);

        // Then
        Assert.AreEqual("review_unavailable", review.Result);
        Assert.IsEmpty(review.Checks);
    }

    private static Task<ReviewResponse> ReviewAsync(HttpClient gateway)
    {
        var options = new ModelOptions(
            new Uri("http://127.0.0.1:8081/v1/chat/completions"),
            "test",
            null
        );
        var service = new ReferralReviewService(
            new ChatCompletionsClient(gateway, options),
            NullLogger<ReferralReviewService>.Instance
        );
        return service.ReviewAsync(Request, Profile, CancellationToken.None);
    }

    private static HttpClient GatewayReturns(params Finding[] findings)
    {
        var content = JsonSerializer.Serialize(new GatewayFindings(findings), ReviewJson.Options);
        var reply = new GatewayResponse([new ChatChoice(new ChatMessage("assistant", content))]);
        return new HttpClient(
            new GatewayHandler(() =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(reply, options: ReviewJson.Options),
                }
            )
        );
    }

    private sealed class GatewayHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(respond());
    }
}
