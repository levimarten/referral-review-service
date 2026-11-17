namespace ReferralReview.Api.Inference;

internal sealed class ModelOptions(Uri endpoint, string model, string? apiKey)
{
    public Uri Endpoint { get; } = endpoint;
    public string Model { get; } = model;
    public string? ApiKey { get; } = apiKey;

    public static ModelOptions FromEnvironment()
    {
        var configuredEndpoint = Environment.GetEnvironmentVariable("REFERRAL_MODEL_URL");
        var model = Environment.GetEnvironmentVariable("REFERRAL_MODEL_NAME");
        var apiKey = Environment.GetEnvironmentVariable("REFERRAL_MODEL_KEY");
        if (!Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var endpoint) ||
            !(endpoint.Scheme == "https" || (endpoint.Scheme == "http" && endpoint.IsLoopback)) ||
            endpoint.UserInfo.Length > 0 || endpoint.Query.Length > 0 || endpoint.Fragment.Length > 0 ||
            string.IsNullOrWhiteSpace(model) ||
            apiKey?.Any(char.IsWhiteSpace) == true)
        {
            throw new InvalidOperationException(
                "Configure REFERRAL_MODEL_URL and REFERRAL_MODEL_NAME, or use --baseline for the offline demo. " +
                "Use HTTPS or loopback HTTP, keep credentials out of the URL and use a bearer token without whitespace.");
        }

        return new ModelOptions(endpoint, model, apiKey);
    }
}