using CsCheck;

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Fuzz;

/// <summary>
/// Continuous fuzz testing (#228) — the long-running counterpart to
/// <c>DeepBehaviorTests</c>' short, deterministic-seeded property tests
/// (~50 cases each, fits the per-PR budget). These run
/// <see cref="FuzzIterations"/> randomized cases per property (100,000 by
/// default on the weekly schedule; a much smaller default locally/ad hoc
/// so `dotnet test` doesn't hang) to surface edge cases the short version
/// misses. CsCheck was chosen over FsCheck — .NET-native idiomatic API,
/// no F# interop, and stronger shrinking.
/// </summary>
public sealed class FuzzProperties
{
    private static int FuzzIterations => GetEnvInt("FUZZ_ITERATIONS", 1_000);



    // CsCheck's SampleAsync `time` parameter is int seconds, not double —
    // passing a double here silently steers overload resolution onto a
    // completely different SampleAsync overload (one expecting
    // Func<T, Task<string>> instead of Func<T, Task<bool>>), producing
    // confusing cascading "cannot convert bool to string" errors on every
    // return statement in the calling lambda. Keep this int.
    private static int FuzzTimeSeconds => GetEnvInt("FUZZ_TIME_SECONDS", -1);



    private static int GetEnvInt(string name, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
    }



    /// <summary>
    /// ChunkAsync concatenation: flattening every chunk reproduces the
    /// original sequence exactly, and every chunk except possibly the
    /// last has exactly <c>maxChunkSize</c> elements.
    /// </summary>
    [Fact]
    public async Task ChunkAsync_concatenation_reproduces_original_sequence()
    {
        var gen = Gen.Int[-1000, 1000].List[0, 200]
            .Select(Gen.Int[1, 50], (items, chunkSize) => (items, chunkSize));

        await gen.SampleAsync(async t =>
        {
            var (items, chunkSize) = t;

            var chunks = new List<int[]>();
            await foreach (var chunk in ToAsyncEnumerable(items).ChunkAsync(chunkSize))
            {
                chunks.Add([.. chunk]);
            }

            var flattened = chunks.SelectMany(c => c).ToList();
            if (!flattened.SequenceEqual(items))
            {
                return false;
            }

            for (var i = 0; i < chunks.Count - 1; i++)
            {
                if (chunks[i].Length != chunkSize)
                {
                    return false;
                }
            }

            return chunks.Count == 0 || (chunks[^1].Length > 0 && chunks[^1].Length <= chunkSize);
        }, iter: FuzzIterations, time: FuzzTimeSeconds);
    }



    /// <summary>
    /// DoAsync transparency: the sequence DoAsync yields is unchanged
    /// from the source, regardless of what the side-effect action does
    /// with each element (as long as it doesn't throw).
    /// </summary>
    [Fact]
    public async Task DoAsync_yields_source_sequence_unchanged()
    {
        var gen = Gen.Int[-1000, 1000].List[0, 200];

        await gen.SampleAsync(async items =>
        {
            var seenBySideEffect = new List<int>();
            var yielded = new List<int>();

            await foreach (var item in ToAsyncEnumerable(items).DoAsync(x => seenBySideEffect.Add(x)))
            {
                yielded.Add(item);
            }

            return yielded.SequenceEqual(items) && seenBySideEffect.SequenceEqual(items);
        }, iter: FuzzIterations, time: FuzzTimeSeconds);
    }



    /// <summary>
    /// NoneAsync(predicate) is the logical negation of "any element
    /// satisfies the predicate" — the async-stream analogue of
    /// <c>!source.Any(predicate)</c>.
    /// </summary>
    [Fact]
    public async Task NoneAsync_predicate_is_negation_of_any()
    {
        var gen = Gen.Int[-1000, 1000].List[0, 200]
            .Select(Gen.Int[-1000, 1000], (items, threshold) => (items, threshold));

        await gen.SampleAsync(async t =>
        {
            var (items, threshold) = t;
            bool Predicate(int x) => x > threshold;

            var actual = await ToAsyncEnumerable(items).NoneAsync(Predicate);
            var expected = !items.Any(Predicate);

            return actual == expected;
        }, iter: FuzzIterations, time: FuzzTimeSeconds);
    }



    /// <summary>
    /// IsEmptyAsync is exactly <c>count == 0</c>.
    /// </summary>
    [Fact]
    public async Task IsEmptyAsync_matches_count_equals_zero()
    {
        var gen = Gen.Int[-1000, 1000].List[0, 200];

        await gen.SampleAsync(async items =>
        {
            var actual = await ToAsyncEnumerable(items).IsEmptyAsync();
            return actual == (items.Count == 0);
        }, iter: FuzzIterations, time: FuzzTimeSeconds);
    }



    private static async IAsyncEnumerable<int> ToAsyncEnumerable(IReadOnlyList<int> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
