namespace ReferralReview.Contracts;

public sealed record Finding(string Check, string Status, string? SourceId, string? Excerpt);
