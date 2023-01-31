using System.Threading.RateLimiting;
using ConsoleApp5;


    // Create an HTTP client with the client-side rate limited handler.
using HttpClient client = new();

switch (args[0])
{
    case "sliding-window":
        await TestSlidingWindowRateLimiting(client);
        break;
    case "middleware":
        await TestMiddlewareRateLimiting(client);
        break;
    case "client-side":
        await TestClientSideRateLimiting();
        break;
    default:
        await TestBucketOrFixedWindowOrConcurrencyRateLimiting(client);
        break;
}


static async Task TestMiddlewareRateLimiting(HttpClient client)
{
    var oneHundredUrls = Enumerable.Range(0, 100).Select(
        i => $"https://localhost:7198/api/rateLimit/middleware?iteration={i}");

    var sendOneThroughNineteenRequests = Parallel.ForEachAsync(
        source: oneHundredUrls.Take(0..19),
        body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));

    await sendOneThroughNineteenRequests;
}

static async Task TestSlidingWindowRateLimiting(HttpClient client)
{
    var oneHundredUrls = Enumerable.Range(0, 100).Select(
        i => $"https://localhost:7198/api/rateLimit/httpclient?iteration={i}");

    var firstRound = Parallel.ForEachAsync(
        source: oneHundredUrls.Take(0..3),
        body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));
    await firstRound;

    Thread.Sleep(17000);

    var scndRound = Parallel.ForEachAsync(
        source: oneHundredUrls.Take(4..5),
        body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));
    await scndRound;

    Thread.Sleep(17000);

    var thirdRound = Parallel.ForEachAsync(
        source: oneHundredUrls.Take(5..6),
        body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));
    await thirdRound;

    Thread.Sleep(34000);

    var fourthRound = Parallel.ForEachAsync(
        source: oneHundredUrls.Take(6..11),
        body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));
    await fourthRound;
}

static async Task TestBucketOrFixedWindowOrConcurrencyRateLimiting(HttpClient client)
{
    var oneHundredUrls = Enumerable.Range(0, 100).Select(
        i => $"https://localhost:7198/api/rateLimit/httpclient?iteration={i}");

    var sendOneThroughNineteenRequests = Parallel.ForEachAsync(
        source: oneHundredUrls.Take(0..19),
        body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));

    await sendOneThroughNineteenRequests;
}

static async Task TestClientSideRateLimiting()
{
    var options = new TokenBucketRateLimiterOptions
    {
        TokenLimit = 3,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 3,
        ReplenishmentPeriod = TimeSpan.FromSeconds(20),
        TokensPerPeriod = 1,
        AutoReplenishment = true
    };

    using HttpClient client = new(
        handler: new ClientSideRateLimitedHandler(
            limiter: new TokenBucketRateLimiter(options)));

    var oneHundredUrls = Enumerable.Range(0, 100).Select(
        i => $"https://localhost:7198/api/rateLimit/httpclient?iteration={i}");

    var sendOneThroughNineteenRequests = Parallel.ForEachAsync(
        source: oneHundredUrls.Take(0..19),
        body: (url, cancellationToken) => GetAsync(client, url, cancellationToken));

    await sendOneThroughNineteenRequests;
}

static async ValueTask GetAsync(
    HttpClient client, string url, CancellationToken cancellationToken)
{
    using var response =
        await client.GetAsync(url, cancellationToken);

    Console.WriteLine(
        $"URL: {url}, HTTP status code: {response.StatusCode} ({(int)response.StatusCode})");
}
