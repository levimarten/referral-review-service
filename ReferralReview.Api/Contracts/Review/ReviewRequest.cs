namespace ReferralReview.Contracts;

public sealed record ReviewRequest(
    string Profile,
    string ReferralType,
    string Draft,
    Source[] SupportingSources
);

public sealed record Source(string Id, string Text);
