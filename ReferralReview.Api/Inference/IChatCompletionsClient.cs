using ReferralReview.Contracts;

namespace ReferralReview.Api.Inference;

internal interface IChatCompletionsClient
{
    Task<Finding[]> InferAsync(GatewayReviewInput input, CancellationToken cancellation);
}
