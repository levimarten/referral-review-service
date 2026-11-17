using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReferralReview.Api.Inference;
using ReferralReview.Api.Reviewing;
using ReferralReview.Contracts;

namespace ReferralReview.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ReviewApiTests
{
    private static readonly Lazy<FixtureSet> Fixtures = new(() =>
        JsonSerializer.Deserialize<FixtureSet>(
            File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "referral-evaluation.json")
            ),
            ReviewJson.Options
        ) ?? throw new InvalidOperationException("Could not load referral cases.")
    );

    public static IEnumerable<object[]> CaseIds() =>
        Fixtures.Value.Cases.Select(item => new object[] { item.Id });

    [TestMethod]
    [DynamicData(nameof(CaseIds))]
    public async Task ReferralMatchesExpectedReview(string caseId)
    {
        // Given
        var fixture = Fixtures.Value.Cases.Single(item => item.Id == caseId);
        var findings = fixture
            .Expected.Checks.Select(check => new Finding(
                check.Key,
                check.Value.Status,
                check.Value.SourceId,
                check.Value.Excerpt
            ))
            .ToArray();
        using var api = new ReviewApiFactory(() => new GatewayResponseHandler(findings));
        using var client = api.CreateClient();

        // When
        using var response = await client.PostAsJsonAsync("/review", fixture.Request);

        // Then
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.CacheControl?.NoStore == true);
        var review = await response.Content.ReadFromJsonAsync<ReviewResponse>(ReviewJson.Options);
        Assert.IsNotNull(review);
        Assert.AreEqual(fixture.Expected.Result, review.Result);
        CollectionAssert.AreEquivalent(
            fixture.Expected.Checks.Keys.ToArray(),
            review.Checks.Keys.ToArray()
        );
        foreach (var (name, expected) in fixture.Expected.Checks)
            Assert.AreEqual(expected.Status, review.Checks[name].Status, $"{caseId}: {name}");
    }

    [TestMethod]
    public async Task EmptyDraftReturnsBadRequest()
    {
        // Given
        var request = Fixtures.Value.Cases[0].Request with { Draft = "" };
        using var api = new ReviewApiFactory(() => new GatewayResponseHandler([]));
        using var client = api.CreateClient();

        // When
        using var response = await client.PostAsJsonAsync("/review", request);

        // Then
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task InventedGatewayQuoteIsHidden()
    {
        // Given
        var request = Fixtures.Value.Cases.Single(item => item.Id == "C08").Request;
        var finding = new Finding("ecg", "present", "draft", "ECG today: normal.");
        using var api = new ReviewApiFactory(() => new GatewayResponseHandler([finding]));
        using var client = api.CreateClient();

        // When
        using var response = await client.PostAsJsonAsync("/review", request);

        // Then
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var review = await response.Content.ReadFromJsonAsync<ReviewResponse>(ReviewJson.Options);
        Assert.IsNotNull(review);
        Assert.AreEqual("review_suggested", review.Result);
        Assert.AreEqual("unable_to_verify", review.Checks["ecg"].Status);
        Assert.IsNull(review.Checks["ecg"].Excerpt);
    }

    private sealed class ReviewApiFactory(Func<HttpMessageHandler> gatewayHandler)
        : WebApplicationFactory<ReferralReviewService>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ModelOptions>();
                services.AddSingleton(
                    new ModelOptions(
                        new Uri("https://api.openai.com/v1/chat/completions"),
                        "test-model",
                        "test-key"
                    )
                );
                services
                    .AddHttpClient<IChatCompletionsClient, ChatCompletionsClient>()
                    .ConfigurePrimaryHttpMessageHandler(gatewayHandler);
            });
    }

    private sealed class GatewayResponseHandler(Finding[] findings) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Assert.AreEqual("api.openai.com", request.RequestUri?.Host);
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            Assert.AreEqual("test-key", request.Headers.Authorization?.Parameter);
            var payload = await request.Content!.ReadFromJsonAsync<GatewayRequest>(
                ReviewJson.Options,
                cancellationToken
            );
            Assert.IsNotNull(payload);
            Assert.AreEqual(false, payload.Store);
            Assert.AreEqual("json_schema", payload.ResponseFormat?.GetProperty("type").GetString());

            var message = new ChatMessage(
                "assistant",
                JsonSerializer.Serialize(new GatewayFindings(findings), ReviewJson.Options)
            );
            var reply = new GatewayResponse([new ChatChoice(message)]);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(reply, options: ReviewJson.Options),
            };
        }
    }

    private sealed record FixtureSet(ReviewCase[] Cases);

    private sealed record ReviewCase(string Id, ReviewRequest Request, ExpectedReview Expected);

    private sealed record ExpectedReview(string Result, Dictionary<string, Evidence> Checks);
}
