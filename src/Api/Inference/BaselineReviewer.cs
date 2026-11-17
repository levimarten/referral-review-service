using ReferralReview.Contracts;

namespace ReferralReview.Api.Inference;

internal sealed class BaselineReviewer : IInferenceClient
{
    public string Mode => "baseline";

    public Task<Finding[]> InferAsync(string profileName, Profile profile, IReadOnlyDictionary<string, string> sources, CancellationToken cancellation) =>
        Task.FromResult(profile.Checks.Keys.Select(check => Check(profileName, check, sources)).ToArray());

    private static Finding Check(string profile, string check, IReadOnlyDictionary<string, string> sources)
    {
        string[] terms = check switch
        {
            "question" => ["?"],
            "medication" => ["medication", "amlodipine"],
            "ecg" => ["ecg"],
            "function" => ["stairs", "walking", "coat", "activities"],
            "imaging" => ["x-ray", "imaging"],
            _ => []
        };
        foreach (var (id, text) in sources)
        {
            foreach (var raw in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                bool Has(string term) => line.Contains(term, StringComparison.OrdinalIgnoreCase);
                if (!terms.Any(Has))
                    continue;

                var status = (check == "medication" && Has("previously")) || Has("unclear") ||
                    (check == "function" && Has("daily activities are affected")) ||
                    (check == "question" && !Has(profile))
                    ? "unclear"
                    : (check is "ecg" or "imaging") && (Has("requested") || Has("planned"))
                        ? "missing"
                        : "present";
                return new Finding(check, status, id, line);
            }
        }

        return new Finding(check, check == "question" ? "unclear" : "missing", null, null);
    }
}
