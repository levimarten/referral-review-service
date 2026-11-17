using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal static class ReviewSources
{
    public static Dictionary<string, string> Build(Referral request) =>
        new[] { new Source("draft", request.Draft) }.Concat(request.SupportingSources)
            .ToDictionary(source => source.Id, source => source.Text, StringComparer.Ordinal);
}
