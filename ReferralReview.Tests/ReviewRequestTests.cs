using ReferralReview.Api.Reviewing;
using ReferralReview.Contracts;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace ReferralReview.Tests;

[TestClass]
public sealed class ReviewRequestTests
{
    [TestMethod]
    public void ValidDraftIsAccepted()
    {
        // Given
        var request = Draft();

        // When
        var accepted = ReviewEndpoints.IsValidRequest(request);

        // Then
        Assert.IsTrue(accepted);
    }

    [TestMethod]
    public void ReservedSourceIdIsRejected()
    {
        // Given
        var request = Draft() with { SupportingSources = [new("draft", "Extra note.")] };

        // When
        var accepted = ReviewEndpoints.IsValidRequest(request);

        // Then
        Assert.IsFalse(accepted);
    }

    [TestMethod]
    public void DuplicateSourceIdsAreRejected()
    {
        // Given
        var request = Draft() with
        {
            SupportingSources = [new("report", "First note."), new("report", "Second note.")],
        };

        // When
        var accepted = ReviewEndpoints.IsValidRequest(request);

        // Then
        Assert.IsFalse(accepted);
    }

    [TestMethod]
    public void OversizedDraftIsRejected()
    {
        // Given
        var request = Draft() with { Draft = new string('x', 12_001) };

        // When
        var accepted = ReviewEndpoints.IsValidRequest(request);

        // Then
        Assert.IsFalse(accepted);
    }

    private static ReviewRequest Draft() =>
        new("cardiology", "specialist_consultation", "Please review this draft.", []);
}
