namespace ReferralReview.Api.Inference;

internal sealed class ModelOptions(Uri endpoint, string model, string? apiKey)
{
    public Uri Endpoint { get; } = endpoint;
    public string Model { get; } = model;
    public string? ApiKey { get; } = apiKey;
    public bool UsesOpenAi => IsOpenAiEndpoint(Endpoint);

    public static ModelOptions FromConfiguration(IConfiguration configuration, string contentRoot)
    {
        if (
            !Uri.TryCreate(configuration["Inference:Endpoint"], UriKind.Absolute, out var endpoint)
            || !(endpoint.Scheme == "https" || (endpoint.Scheme == "http" && endpoint.IsLoopback))
            || endpoint.UserInfo.Length > 0
            || endpoint.Query.Length > 0
            || endpoint.Fragment.Length > 0
        )
        {
            throw new InvalidOperationException(
                "Set Inference:Endpoint to an HTTPS or loopback HTTP URL without credentials."
            );
        }

        var model = configuration["Inference:Model"];
        var openAi = IsOpenAiEndpoint(endpoint);
        string? apiKey;
        if (openAi)
        {
            var local = ReadLocalSettings(contentRoot);
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? local.GetValueOrDefault("OPENAI_API_KEY");
            model = Environment.GetEnvironmentVariable("OPENAI_MODEL")
                ?? local.GetValueOrDefault("OPENAI_MODEL")
                ?? model;
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Set OPENAI_API_KEY in the environment or the repository's ignored .env file."
                );
        }
        else
        {
            apiKey = Environment.GetEnvironmentVariable("REFERRAL_MODEL_KEY");
        }

        if (string.IsNullOrWhiteSpace(model) || apiKey?.Any(char.IsWhiteSpace) == true)
            throw new InvalidOperationException(
                "Set the inference model and supply any API key without whitespace."
            );

        return new ModelOptions(endpoint, model, apiKey);
    }

    private static bool IsOpenAiEndpoint(Uri endpoint) =>
        endpoint.Scheme == "https"
        && endpoint.IsDefaultPort
        && endpoint.Host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, string> ReadLocalSettings(string contentRoot)
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal);
        for (
            var directory = new DirectoryInfo(contentRoot);
            directory is not null;
            directory = directory.Parent
        )
        {
            if (!File.Exists(Path.Combine(directory.FullName, "ReferralReview.sln")))
                continue;

            var path = Path.Combine(directory.FullName, ".env");
            if (File.Exists(path))
            {
                foreach (var line in File.ReadLines(path))
                {
                    var pair = line.Split('=', 2);
                    if (
                        pair is [var name, var value]
                        && name.Trim() is ("OPENAI_API_KEY" or "OPENAI_MODEL")
                    )
                        settings[name.Trim()] = value.Trim().Trim('"', '\'');
                }
            }
            break;
        }

        return settings;
    }
}
