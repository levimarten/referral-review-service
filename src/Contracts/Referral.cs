namespace ReferralReview.Contracts;

public sealed record Referral(string Profile, string ReferralType, string Draft, Source[] SupportingSources);
