using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReferralReview.Contracts;

public sealed record GatewayRequest(
    string Model,
    ChatMessage[] Messages,
    [property:
        JsonPropertyName("response_format"),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)
    ]
        JsonElement? ResponseFormat = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Store = null,
    [property:
        JsonPropertyName("max_completion_tokens"),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)
    ]
        int? MaxCompletionTokens = null
);

public sealed record ChatMessage(string Role, string? Content);

public sealed record GatewayReviewInput(
    string Profile,
    Dictionary<string, string> Checklist,
    IReadOnlyDictionary<string, string> Sources
);
