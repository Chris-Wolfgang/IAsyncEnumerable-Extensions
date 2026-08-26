using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Wolfgang.Extensions.IAsyncEnumerable;

namespace ShadowWorkloads;

/// <summary>
/// Realistic scenario 2: an HTTP-paged API response streamed as
/// <see cref="IAsyncEnumerable{T}"/>, with <c>DoAsync</c> recording a
/// telemetry side-effect (e.g. a metrics counter) on every item without
/// disturbing the pass-through sequence, terminally consumed via
/// <c>ForEachAsync</c>. Also doubles as usage documentation for the
/// DoAsync-then-ForEachAsync composition.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class HttpPagedTelemetryScenario
{
    private const int ItemCount = 5_000;



    [Benchmark]
    public async Task<int> Scenario_PagedApiItemsWithTelemetrySideEffect()
    {
        var telemetryCount = 0;
        var processedCount = 0;

        await PagedApiItems(ItemCount)
            .DoAsync(_ => telemetryCount++)
            .ForEachAsync(_ => processedCount++)
            .ConfigureAwait(false);

        return telemetryCount + processedCount;
    }



    private static async IAsyncEnumerable<ApiItem> PagedApiItems(int itemCount)
    {
        const int pageSize = 100;

        for (var offset = 0; offset < itemCount; offset += pageSize)
        {
            await Task.Yield();  // models an HTTP round trip per page

            var pageLength = Math.Min(pageSize, itemCount - offset);
            for (var i = 0; i < pageLength; i++)
            {
                var id = offset + i;
                yield return new ApiItem(id, $"payload-{id}");
            }
        }
    }



    private readonly record struct ApiItem(int Id, string Payload);
}
