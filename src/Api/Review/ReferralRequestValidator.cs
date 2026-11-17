using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal static class ReferralRequestValidator
{
    public static bool IsValid(Referral request)
    {
        if (string.IsNullOrWhiteSpace(request.Draft) || request.Draft.Length > 12_000 ||
            request.SupportingSources is not { Length: <= 5 })
            return false;

        var ids = new HashSet<string>(StringComparer.Ordinal) { "draft" };
        return request.SupportingSources.All(source =>
            source is not null && source.Id is { Length: > 0 and <= 80 } &&
            source.Id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_') &&
            source.Text is { Length: <= 4_000 } && !string.IsNullOrWhiteSpace(source.Text) &&
            ids.Add(source.Id));
    }
}