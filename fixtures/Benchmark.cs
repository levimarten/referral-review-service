using System.Text.Json;
using ReferralReview.Contracts;

internal sealed record Fixture(string Id, string Scenario, Referral Request, Expected Expected);
internal sealed record Expected(string Result, Dictionary<string, Evidence> Checks);
internal sealed record Fault(string Id, string CaseId, Finding? ModelFinding, string? ExpectedStatus);
internal sealed record Benchmark(Dictionary<string, Profile> Profiles, Fixture[] Cases, Fault[] FaultFixtures)
{
    public static Benchmark Load() =>
        JsonSerializer.Deserialize<Benchmark>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "referral-evaluation.json")), ReviewJson.Options)
        ?? throw new InvalidOperationException("Unable to load evaluation fixtures.");
}