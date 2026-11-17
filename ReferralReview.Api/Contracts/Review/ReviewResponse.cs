namespace ReferralReview.Contracts;

public sealed record ReviewResponse(
    string ReviewId,
    string Result,
    string Mode,
    Dictionary<string, Evidence> Checks
);

public sealed record Evidence(string Status, string? SourceId, string? Excerpt);
