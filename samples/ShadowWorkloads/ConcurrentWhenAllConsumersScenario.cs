using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Wolfgang.Extensions.IAsyncEnumerable;

namespace ShadowWorkloads;

/// <summary>
/// Realistic scenario 4: several independent consumers, each paging their
/// own data feed and chunking it for downstream processing, running
/// concurrently via <c>Task.WhenAll</c> — the shape of a fan-out worker
/// pool (e.g. one task per tenant/shard). Also doubles as usage
/// documentation for driving multiple independent <c>ChunkAsync</c>
/// pipelines concurrently.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ConcurrentWhenAllConsumersScenario
{
    private const int ConsumerCount = 8;
    private const int ItemsPerConsumer = 1_000;



    [Benchmark]
    public async Task<int> Scenario_ConcurrentIndependentConsumers()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, ConsumerCount).Select(async consumerId =>
        {
            var itemCount = 0;

            await foreach (var chunk in FeedFor(consumerId, ItemsPerConsumer).ChunkAsync(50).ConfigureAwait(false))
            {
                itemCount += chunk.Count;
            }

            return itemCount;
        })).ConfigureAwait(false);

        return results.Sum();
    }



    private static async IAsyncEnumerable<int> FeedFor(int consumerId, int itemCount)
    {
        for (var i = 0; i < itemCount; i++)
        {
            if (i % 50 == 0)
            {
                await Task.Yield();
            }

            yield return (consumerId * itemCount) + i;
        }
    }
}
