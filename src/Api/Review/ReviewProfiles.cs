using System.Text.Json;
using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal sealed class ReviewProfiles(Dictionary<string, Profile> profiles)
{
    public static ReviewProfiles Load()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "review-profiles.json"));
        var profiles = JsonSerializer.Deserialize<Dictionary<string, Profile>>(stream, ReviewJson.Options);
        if (profiles is not { Count: > 0 } || profiles.Values.Any(profile =>
                profile is null || string.IsNullOrWhiteSpace(profile.ReferralType) ||
                profile.Checks is not { Count: > 0 } || profile.Checks.Any(check =>
                    string.IsNullOrWhiteSpace(check.Key) || string.IsNullOrWhiteSpace(check.Value))))
            throw new InvalidOperationException("Review profiles must have a referral type and at least one named check.");

        return new ReviewProfiles(profiles);
    }

    public Profile? Resolve(string? name, string? referralType) =>
        name is not null && profiles.TryGetValue(name, out var profile) && profile.ReferralType == referralType
            ? profile
            : null;
}