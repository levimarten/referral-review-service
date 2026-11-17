namespace ReferralReview.Contracts;

public sealed record Profile(string ReferralType, Dictionary<string, string> Checks);
