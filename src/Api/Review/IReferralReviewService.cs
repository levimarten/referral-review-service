using ReferralReview.Contracts;

namespace ReferralReview.Api.Reviewing;

internal interface IReferralReviewService
{
    Task<Review> ReviewAsync(Referral request, Profile profile, CancellationToken cancellation);
}