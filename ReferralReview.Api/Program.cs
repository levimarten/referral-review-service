using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ReferralReview.Api.Inference;
using ReferralReview.Api.Reviewing;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration["AllowedHosts"] = "localhost;127.0.0.1";
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, 5077);
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 256 * 1024;
});
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.MaxDepth = 24);
builder.Services.AddSingleton(ReviewProfiles.Load());
builder.Services.AddSingleton<IReferralReviewService, ReferralReviewService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddConcurrencyLimiter(ReviewEndpoints.InferenceLimit, limiter =>
    {
        limiter.PermitLimit = 4;
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddSingleton(services =>
    ModelOptions.FromConfiguration(
        services.GetRequiredService<IConfiguration>(),
        services.GetRequiredService<IHostEnvironment>().ContentRootPath
    )
);
builder
    .Services.AddHttpClient<IChatCompletionsClient, ChatCompletionsClient>(client =>
        client.Timeout = Timeout.InfiniteTimeSpan
    )
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
    .RemoveAllLoggers();

var app = builder.Build();
_ = app.Services.GetRequiredService<ModelOptions>();
app.Use(
    async (context, next) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        await next(context);
    }
);
app.UseRateLimiter();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapReviewEndpoints();
await app.RunAsync();
