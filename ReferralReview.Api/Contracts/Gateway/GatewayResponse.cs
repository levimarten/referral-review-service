namespace ReferralReview.Contracts;

public sealed record GatewayResponse(ChatChoice[]? Choices);

public sealed record ChatChoice(ChatMessage? Message);

public sealed record GatewayFindings(Finding[]? Findings);

public sealed record Finding(string Check, string Status, string? SourceId, string? Excerpt);
