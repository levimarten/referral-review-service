namespace ReferralReview.Contracts;

public sealed record Review(string ReviewId, string Result, string Mode, Dictionary<string, Evidence> Checks);
