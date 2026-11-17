using ReferralReview.Contracts;
using System.Net.Http.Json;
using System.Text.Json;

var benchmark = Benchmark.Load();
if (args is not (["--evaluate"] or [not null] or [not null, "--send-now"]))
{
    Console.Error.WriteLine("Usage: Cli CASE_ID [--send-now] or Cli --evaluate. Examples: C01, C05, O02.");
    return 1;
}

using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5077"), Timeout = TimeSpan.FromSeconds(15) };
if (args[0] == "--evaluate")
{
    var correct = 0;
    var total = 0;
    var unavailable = 0;
    var differingPositiveEvidence = 0;
    var affirmationsWithoutExpectedSupport = 0;
    foreach (var item in benchmark.Cases)
    {
        var result = await RequestReview(item.Request, CancellationToken.None);
        Console.WriteLine($"{item.Id} {result?.Mode ?? "offline"}: {result?.Result ?? "review_unavailable"}");
        if (result is null || result.Result == "review_unavailable")
            unavailable++;
        foreach (var (key, expected) in item.Expected.Checks)
        {
            total++;
            Evidence? actual = null;
            result?.Checks.TryGetValue(key, out actual);
            if (actual?.Status == expected.Status)
            {
                correct++;
                if (actual is { Status: "present" } &&
                    (actual.SourceId != expected.SourceId || actual.Excerpt != expected.Excerpt))
                {
                    differingPositiveEvidence++;
                    Console.WriteLine($"  {key}: positive evidence differs from the reference excerpt.");
                }
            }
            else
            {
                Console.WriteLine($"  {key}: expected {expected.Status}, got {actual?.Status ?? "no result"}");
                if (actual?.Status == "present")
                    affirmationsWithoutExpectedSupport++;
            }
        }
    }
    Console.WriteLine($"Checklist status matches: {correct}/{total}. Unexpected present labels: {affirmationsWithoutExpectedSupport}. Differing positive excerpts: {differingPositiveEvidence}. Unavailable reviews: {unavailable}/{benchmark.Cases.Length}.");
    return unavailable == 0 ? 0 : 2;
}

var selected = benchmark.Cases.FirstOrDefault(item => item.Id.Equals(args[0], StringComparison.OrdinalIgnoreCase));
if (selected is null)
{
    Console.Error.WriteLine($"Unknown case. Available cases: {string.Join(", ", benchmark.Cases.Select(item => item.Id))}.");
    return 1;
}

Console.WriteLine(selected.Request.Draft);
Console.WriteLine("\nChecking draft. Press Enter to send the referral in the simulation.");
using var reviewCancellation = new CancellationTokenSource();
var pendingReview = RequestReview(selected.Request, reviewCancellation.Token);
var reviewTask = Task.Run(async () =>
{
    var result = await pendingReview;
    if (reviewCancellation.IsCancellationRequested)
        return;
    if (result is null || result.Result == "review_unavailable")
    {
        Console.WriteLine("Review unavailable. Send remains available.");
        return;
    }
    Console.WriteLine(result.Result == "no_gaps_found"
        ? "No gaps found in reviewed items."
        : "Review suggested.");
    foreach (var (check, evidence) in result.Checks)
    {
        Console.WriteLine($"  {check}: {evidence.Status.Replace('_', ' ')}");
        if (evidence.Excerpt is not null)
            Console.WriteLine($"    [{evidence.SourceId}] {evidence.Excerpt}");
    }
    Console.WriteLine($"Review ID: {result.ReviewId} ({result.Mode})");
});

var send = args.Length == 2 || Console.ReadLine() is not null;
Console.WriteLine(send ? "Sent to specialist (simulation)." : "Session closed.");
await reviewCancellation.CancelAsync();
await reviewTask;
return 0;

async Task<Review?> RequestReview(Referral request, CancellationToken cancellation)
{
    try
    {
        using var response = await client.PostAsJsonAsync("/review", request, ReviewJson.Options, cancellation);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Review>(ReviewJson.Options, cancellation);
    }
    catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException)
    {
        return null;
    }
}