using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal static class EvidenceValidator
{
    public static Evidence Validate(Finding? finding, IReadOnlyDictionary<string, string> sources)
    {
        var unverified = new Evidence("unable_to_verify", null, null);
        if (finding?.Status is not ("present" or "unclear" or "missing" or "unable_to_verify"))
            return unverified;

        // An absent item may name the source that was searched without citing a passage.
        if (
            finding.Status == "missing"
            && string.IsNullOrWhiteSpace(finding.Excerpt)
            && (finding.SourceId is null || sources.ContainsKey(finding.SourceId))
        )
            return new Evidence("missing", null, null);

        if (finding.SourceId is null && finding.Excerpt is null && finding.Status != "present")
            return new Evidence(finding.Status, null, null);

        if (
            finding.SourceId is null
            || string.IsNullOrWhiteSpace(finding.Excerpt)
            || !sources.TryGetValue(finding.SourceId, out var source)
        )
            return unverified;

        var start = source.IndexOf(finding.Excerpt, StringComparison.Ordinal);
        if (start < 0)
            return unverified;

        if (finding.Status == "present")
        {
            // The excerpt must identify one passage. Check the surrounding lines for
            // explicit unavailability cues even if the model quotes only part of a line.
            if (source.IndexOf(finding.Excerpt, start + 1, StringComparison.Ordinal) >= 0)
                return unverified;
            var end = start + finding.Excerpt.Length;
            var contextStart = start == 0 ? 0 : source.LastIndexOf('\n', start - 1) + 1;
            var nextLine = source.IndexOf('\n', end);
            var context = source.AsSpan(
                contextStart,
                (nextLine < 0 ? source.Length : nextLine) - contextStart
            );
            if (
                context.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
                || context.Contains("not available", StringComparison.OrdinalIgnoreCase)
            )
                return unverified;
        }

        return new Evidence(finding.Status, finding.SourceId, finding.Excerpt);
    }
}
