using ReferralReview.Contracts;

namespace ReferralReview.Api.Inference;

internal interface IInferenceClient
{
    string Mode { get; }

    Task<Finding[]> InferAsync(string profileName, Profile profile,
        IReadOnlyDictionary<string, string> sources, CancellationToken cancellation);
}