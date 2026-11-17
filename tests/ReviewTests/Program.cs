using Microsoft.Extensions.Logging.Abstractions;
using ReferralReview.Api.Inference;
using ReferralReview.Api.Reviewing;
using ReferralReview.Contracts;

var benchmark = Benchmark.Load();
var assertions = 0;

foreach (var fault in benchmark.FaultFixtures.Where(fault => fault.ModelFinding is not null))
{
    var fixture = benchmark.Cases.Single(item => item.Id == fault.CaseId);
    var finding = fault.ModelFinding!;
    var review = await Evaluate(fixture, [finding]);
    Assert(review.Checks[finding.Check].Status == fault.ExpectedStatus &&
           review.Checks[finding.Check].Excerpt is null, fault.Id);
}

var partial = EvidenceValidator.Validate(new Finding("medication", "present", "draft", "Medication list"),
    new Dictionary<string, string> { ["draft"] = "Medication list unavailable." });
Assert(partial.Status == "unable_to_verify", "partial quote omits unavailability");
var multiline = EvidenceValidator.Validate(new Finding("medication", "present", "draft",
    "Medication list\nunavailable."), new Dictionary<string, string> { ["draft"] = "Medication list\nunavailable." });
Assert(multiline.Status == "unable_to_verify", "multiline quote contains unavailability");
var repeated = EvidenceValidator.Validate(new Finding("medication", "present", "draft", "Medication list"),
    new Dictionary<string, string> { ["draft"] = "Medication list unavailable.\nMedication list attached." });
Assert(repeated.Status == "unable_to_verify", "ambiguous repeated excerpt");

var complete = benchmark.Cases.Single(item => item.Id == "C01");
var supported = complete.Expected.Checks.Select(item =>
    new Finding(item.Key, item.Value.Status, item.Value.SourceId, item.Value.Excerpt)).ToArray();
Assert((await Evaluate(complete, supported)).Result == "no_gaps_found", "complete referral");
Assert((await Evaluate(complete, [..supported, supported[0]])).Checks[supported[0].Check].Status ==
       "unable_to_verify", "duplicate finding");
Assert((await Evaluate(complete, [])).Checks.Values.All(item => item.Status == "unable_to_verify"), "missing model findings");
Assert(ReferralRequestValidator.IsValid(complete.Request), "valid input");
Assert(!ReferralRequestValidator.IsValid(complete.Request with { SupportingSources = [new("draft", "other")] }),
    "reserved source ID");
Assert(!ReferralRequestValidator.IsValid(complete.Request with { SupportingSources = [new("same", "one"), new("same", "two")] }),
    "duplicate source ID");
Assert(!ReferralRequestValidator.IsValid(complete.Request with { Draft = new string('x', 12_001) }), "oversize draft");

var failure = new ReferralReviewService(new FixedInference(() => throw new HttpRequestException()),
    NullLogger<ReferralReviewService>.Instance);
Assert((await failure.ReviewAsync(complete.Request, benchmark.Profiles[complete.Request.Profile], default)).Result ==
       "review_unavailable", "upstream failure");

Console.WriteLine($"Passed {assertions} review assertions.");

async Task<Review> Evaluate(Fixture fixture, Finding[] findings)
{
    var service = new ReferralReviewService(new FixedInference(() => findings), NullLogger<ReferralReviewService>.Instance);
    return await service.ReviewAsync(fixture.Request, benchmark.Profiles[fixture.Request.Profile], default);
}

void Assert(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"Failed: {name}");
    assertions++;
    Console.WriteLine($"Passed: {name}");
}

internal sealed class FixedInference(Func<Finding[]> response) : IInferenceClient
{
    public string Mode => "test";

    public Task<Finding[]> InferAsync(string profileName, Profile profile,
        IReadOnlyDictionary<string, string> sources, CancellationToken cancellation) =>
        Task.FromResult(response());
}