using System.Text.Json;

namespace ReferralReview.Contracts;

public static class ReviewJson
{
    public static JsonSerializerOptions Options { get; } =
        new(JsonSerializerDefaults.Web) { MaxDepth = 24 };
}
