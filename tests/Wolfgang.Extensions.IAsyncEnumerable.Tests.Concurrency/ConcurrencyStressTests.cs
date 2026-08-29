namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Concurrency;

/// <summary>
/// Systematic schedule-exploration stress tests (#233). Unlike
/// <c>DeepBehaviorTests</c>' single-threaded disposal-semantics tests, these
/// run many independent consumers concurrently via the real thread pool to
/// surface interleaving-dependent bugs that a single deterministic run
/// won't. Every method under test here has no shared mutable state — no
/// static field, no cache — so each test's job is to empirically confirm
/// that invariant under contention, and to regress loudly if a future
/// change introduces shared state.
/// </summary>
public sealed class ConcurrencyStressTests
{
    // STRESS_ITERATIONS scales the round count: modest by default (fast,
    // runs in every `dotnet test`), generous when the weekly workflow sets
    // it explicitly. Coyote was considered and skipped — rough
    // IAsyncEnumerable support and a net8.0-only CLI make it a poor fit
    // here; this xunit-based stress suite is the gate instead.
    private static readonly int StressIterations = GetStressIterations();



    private static int GetStressIterations()
    {
        var raw = Environment.GetEnvironmentVariable("STRESS_ITERATIONS");
        return int.TryParse(raw, out var parsed) && parsed > 0
            ? parsed
            : 100;
    }



    private static readonly int ConcurrentConsumers = Math.Clamp(Environment.ProcessorCount * 2, 4, 16);



    [Fact]
    public async Task ChunkAsync_concurrent_independent_consumers_do_not_corrupt_each_other()
    {
        for (var round = 0; round < StressIterations; round++)
        {
            var results = await Task.WhenAll(Enumerable.Range(0, ConcurrentConsumers).Select(async _ =>
            {
                var chunks = new List<int[]>();
                await foreach (var chunk in Range(0, 100).ChunkAsync(7))
                {
                    chunks.Add(chunk.ToArray());
                }

                return chunks;
            }));

            foreach (var chunks in results)
            {
                Assert.Equal(15, chunks.Count);  // ceil(100 / 7)
                Assert.Equal(Enumerable.Range(0, 100).Sum(), chunks.SelectMany(c => c).Sum());
                Assert.Equal(2, chunks[^1].Length);  // 100 % 7 == 2
            }
        }
    }



    [Fact]
    public async Task DoAsync_concurrent_consumers_side_effects_are_isolated_per_consumer()
    {
        for (var round = 0; round < StressIterations; round++)
        {
            var results = await Task.WhenAll(Enumerable.Range(0, ConcurrentConsumers).Select(async consumerId =>
            {
                var seenByThisConsumer = new List<int>();
                await foreach (var item in Range(0, 50).DoAsync(x => seenByThisConsumer.Add(x)))
                {
                    _ = item;
                }

                return (consumerId, seenByThisConsumer);
            }));

            foreach (var (_, seen) in results)
            {
                // Each consumer's own local list must be exactly its own
                // sequence, unaffected by however many siblings ran
                // concurrently — no cross-consumer state leakage.
                Assert.Equal(Enumerable.Range(0, 50), seen);
            }
        }
    }



    [Fact]
    public async Task ForEachAsync_concurrent_consumers_are_correct_under_contention()
    {
        for (var round = 0; round < StressIterations; round++)
        {
            var sums = await Task.WhenAll(Enumerable.Range(0, ConcurrentConsumers).Select(async _ =>
            {
                var sum = 0;
                await Range(1, 30).ForEachAsync(x => Interlocked.Add(ref sum, x));
                return sum;
            }));

            var expected = Enumerable.Range(1, 30).Sum();
            Assert.All(sums, sum => Assert.Equal(expected, sum));
        }
    }



    [Fact]
    public async Task NoneAsync_predicate_concurrent_consumers_are_correct_under_contention()
    {
        for (var round = 0; round < StressIterations; round++)
        {
            var results = await Task.WhenAll(Enumerable.Range(0, ConcurrentConsumers).Select(consumerId =>
                Range(0, 40).NoneAsync(x => x == consumerId % 40)));

            // Every consumer's own predicate targets a value that IS present
            // in its own 0..39 source, so every result must be false —
            // a leaked/racing enumerator from a sibling could flip this.
            Assert.All(results, result => Assert.False(result));
        }
    }



    [Fact]
    public async Task Racing_DisposeAsync_across_independent_enumerators_does_not_affect_sibling_consumers()
    {
        for (var round = 0; round < StressIterations; round++)
        {
            var tasks = Enumerable.Range(0, ConcurrentConsumers).Select(async consumerId =>
            {
                var enumerator = Range(0, 20).GetAsyncEnumerator();
                await using (enumerator.ConfigureAwait(false))
                {
                    // Every third consumer disposes early (via the await-using
                    // block exiting after a partial read) while its siblings
                    // are still mid-iteration on their own independent
                    // enumerator instances.
                    var stopEarly = consumerId % 3 == 0;
                    var sum = 0;
                    var count = 0;
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        sum += enumerator.Current;
                        count++;
                        if (stopEarly && count == 5)
                        {
                            break;
                        }
                    }

                    return (stopEarly, sum, count);
                }
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (stopEarly, sum, count) in results)
            {
                if (stopEarly)
                {
                    Assert.Equal(5, count);
                    Assert.Equal(Enumerable.Range(0, 5).Sum(), sum);
                }
                else
                {
                    Assert.Equal(20, count);
                    Assert.Equal(Enumerable.Range(0, 20).Sum(), sum);
                }
            }
        }
    }



    private static async IAsyncEnumerable<int> Range(int start, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return start + i;
        }
    }
}
