using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal interface IReferralReviewService
{
    Task<ReviewResponse> ReviewAsync(
        ReviewRequest request,
        ReviewProfile profile,
        CancellationToken cancellation
    );
}
