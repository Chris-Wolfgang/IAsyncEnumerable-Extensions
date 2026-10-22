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

            // One expression, so a passing run executes every line: separate
            // `return false` branches would only run when the property fails.
            var flattened = chunks.SelectMany(c => c).ToList();
            var allButLastAreFull = chunks.Take(chunks.Count - 1).All(c => c.Length == chunkSize);
            var lastIsPartial = chunks.Count == 0 || (chunks[^1].Length > 0 && chunks[^1].Length <= chunkSize);

            return flattened.SequenceEqual(items) && allButLastAreFull && lastIsPartial;
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



    /// <summary>
    /// ScanAsync running fold: the i-th yielded value equals <c>Aggregate</c>
    /// over the first i + 1 source elements, and there is exactly one value
    /// per source element (the seed is not yielded).
    /// </summary>
    [Fact]
    public async Task ScanAsync_each_value_equals_aggregate_over_the_prefix()
    {
        var gen = Gen.Int[-1000, 1000].List[0, 200]
            .Select(Gen.Int[-1000, 1000], (items, seed) => (items, seed));

        await gen.SampleAsync(async t =>
        {
            var (items, seed) = t;

            var yielded = new List<long>();
            await foreach (var value in ToAsyncEnumerable(items).ScanAsync((long)seed, static (sum, x) => sum + x))
            {
                yielded.Add(value);
            }

            var expected = Enumerable
                .Range(1, items.Count)
                .Select(n => items.Take(n).Aggregate((long)seed, static (sum, x) => sum + x));

            return yielded.SequenceEqual(expected);
        }, iter: FuzzIterations, time: FuzzTimeSeconds);
    }



    /// <summary>
    /// DistinctUntilChangedAsync: no two adjacent outputs are equal, and the
    /// output is exactly the input with each run of equal adjacent elements
    /// collapsed to its first element.
    /// </summary>
    [Fact]
    public async Task DistinctUntilChangedAsync_collapses_every_run_of_equal_adjacent_elements()
    {
        // A narrow value range so runs of equal neighbours are common.
        var gen = Gen.Int[0, 3].List[0, 200];

        await gen.SampleAsync(async items =>
        {
            var yielded = new List<int>();
            await foreach (var item in ToAsyncEnumerable(items).DistinctUntilChangedAsync())
            {
                yielded.Add(item);
            }

            var expected = items.Where((item, i) => i == 0 || item != items[i - 1]);
            var noAdjacentRepeats = yielded.Zip(yielded.Skip(1), static (a, b) => a != b).All(static different => different);

            return yielded.SequenceEqual(expected) && noAdjacentRepeats;
        }, iter: FuzzIterations, time: FuzzTimeSeconds);
    }



    /// <summary>
    /// SlidingWindowAsync: yields max(0, n - w + 1) windows, and window i is
    /// the contiguous slice <c>items[i .. i + w)</c>.
    /// </summary>
    [Fact]
    public async Task SlidingWindowAsync_windows_are_every_contiguous_slice()
    {
        var gen = Gen.Int[-1000, 1000].List[0, 200]
            .Select(Gen.Int[1, 50], (items, windowSize) => (items, windowSize));

        await gen.SampleAsync(async t =>
        {
            var (items, windowSize) = t;

            var windows = new List<int[]>();
            await foreach (var window in ToAsyncEnumerable(items).SlidingWindowAsync(windowSize))
            {
                windows.Add([.. window]);
            }

            var expectedCount = Math.Max(0, items.Count - windowSize + 1);
            var everyWindowIsItsSlice = windows
                .Select((window, i) => window.SequenceEqual(items.Skip(i).Take(windowSize)))
                .All(static matches => matches);

            return windows.Count == expectedCount && everyWindowIsItsSlice;
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
