using ReferralReview.Api.Reviewing;
using ReferralReview.Contracts;

namespace ReferralReview.Tests;

[TestClass]
public sealed class EvidenceTests
{
    [TestMethod]
    [DataRow("draft", "ECG today: normal.", DisplayName = "Invented quote from an existing source")]
    [DataRow(
        "unknown",
        "ECG today: sinus rhythm.",
        DisplayName = "Real quote attributed to an unknown source"
    )]
    public void BadCitationIsHidden(string sourceId, string excerpt)
    {
        // Given
        var sources = new Dictionary<string, string> { ["draft"] = "ECG today: sinus rhythm." };
        var finding = new Finding("ecg", "present", sourceId, excerpt);

        // When
        var result = EvidenceValidator.Validate(finding, sources);

        // Then
        Assert.AreEqual("unable_to_verify", result.Status);
        Assert.IsNull(result.SourceId);
        Assert.IsNull(result.Excerpt);
    }

    [TestMethod]
    [DataRow(
        "Medication list unavailable.",
        "Medication list unavailable.",
        DisplayName = "Unavailable medication list"
    )]
    [DataRow(
        "Medication list unavailable.",
        "Medication list",
        DisplayName = "Quote leaves out 'unavailable'"
    )]
    [DataRow(
        "Medication list\nunavailable.",
        "Medication list\nunavailable.",
        DisplayName = "Unavailable list across two lines"
    )]
    public void UnavailableInformationDoesNotCountAsPresent(string draft, string excerpt)
    {
        // Given
        var sources = new Dictionary<string, string> { ["draft"] = draft };
        var finding = new Finding("medication", "present", "draft", excerpt);

        // When
        var result = EvidenceValidator.Validate(finding, sources);

        // Then
        Assert.AreEqual("unable_to_verify", result.Status);
        Assert.IsNull(result.Excerpt);
    }

    [TestMethod]
    public void MissingFindingCanNameTheSearchedSourceWithoutAQuote()
    {
        // Given
        var sources = new Dictionary<string, string> { ["draft"] = "Please assess." };
        var finding = new Finding("ecg", "missing", "draft", null);

        // When
        var result = EvidenceValidator.Validate(finding, sources);

        // Then
        Assert.AreEqual("missing", result.Status);
        Assert.IsNull(result.SourceId);
        Assert.IsNull(result.Excerpt);
    }

    [TestMethod]
    public void RepeatedQuoteDoesNotIdentifyOnePassage()
    {
        // Given
        var sources = new Dictionary<string, string>
        {
            ["draft"] = "Medication list unavailable.\nMedication list attached.",
        };
        var finding = new Finding("medication", "present", "draft", "Medication list");

        // When
        var result = EvidenceValidator.Validate(finding, sources);

        // Then
        Assert.AreEqual("unable_to_verify", result.Status);
        Assert.IsNull(result.Excerpt);
    }
}
