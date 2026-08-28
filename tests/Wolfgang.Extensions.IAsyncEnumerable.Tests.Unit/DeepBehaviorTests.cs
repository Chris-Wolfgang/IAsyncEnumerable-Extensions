// Deep-behavior tests added per the thorough code-review on #181.
// Coverage adversarial / concurrent / AsyncLocal-flow / property-based
// scenarios that the per-method test files don't exercise. Each test below
// pins a specific contract the library quietly promises.

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class DeepBehaviorTests
{
    // ----------------------------------------------------------------------
    // Concurrent-disposal failure modes
    // ----------------------------------------------------------------------

    [Fact]
    public async Task ChunkAsync_when_source_throws_mid_iteration_exception_propagates_and_enumerator_is_disposed()
    {
        var sentinel = new InvalidOperationException("source boom");
        var disposeFlag = new DisposeFlag();
        var source = new ThrowingAsyncEnumerable(disposeFlag, throwAt: 5, sentinel);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var chunk in source.ChunkAsync(2))
            {
                _ = chunk;
            }
        });

        Assert.Same(sentinel, thrown);
        Assert.True(disposeFlag.Disposed, "source enumerator must be disposed even when the source throws mid-iteration");
    }



    [Fact]
    public async Task DoAsync_when_consumer_breaks_early_source_enumerator_is_disposed()
    {
        var disposeFlag = new DisposeFlag();
        var source = new ThrowingAsyncEnumerable(disposeFlag, throwAt: -1, exception: null);  // never throws

        await foreach (var item in source.DoAsync(_ => { }))
        {
            if (item >= 3)
            {
                break;  // early termination triggers the await-foreach disposal path
            }
        }

        Assert.True(disposeFlag.Disposed, "early break in await-foreach must dispose the source enumerator");
    }



    [Fact]
    public async Task ForEachAsync_when_user_action_throws_source_enumerator_is_disposed()
    {
        var disposeFlag = new DisposeFlag();
        var source = new ThrowingAsyncEnumerable(disposeFlag, throwAt: -1, exception: null);
        var sentinel = new InvalidOperationException("action boom");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.ForEachAsync(_ => throw sentinel));

        Assert.True(disposeFlag.Disposed, "action exception must not skip enumerator disposal");
    }



    [Fact]
    public async Task ChunkAsync_when_canceled_mid_chunk_propagates_and_disposes()
    {
        // Reach the inner chunk-boundary token check. ChunkSize=2, cancel after yielding chunk 1.
        using var cts = new CancellationTokenSource();
        var disposeFlag = new DisposeFlag();
        var source = new ThrowingAsyncEnumerable(disposeFlag, throwAt: -1, exception: null);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            var chunkCount = 0;
            await foreach (var chunk in source.ChunkAsync(2, cts.Token))
            {
                chunkCount++;
                if (chunkCount == 1)
                {
                    cts.Cancel();
                }
            }
        });

        Assert.True(disposeFlag.Disposed, "mid-iteration cancellation must dispose the source enumerator");
    }



    // ----------------------------------------------------------------------
    // AsyncLocal flow correctness
    // ----------------------------------------------------------------------

    private static readonly AsyncLocal<int> AmbientId = new();

    [Fact]
    public async Task DoAsync_action_observes_caller_AsyncLocal_value()
    {
        AmbientId.Value = 42;
        var observed = new List<int>();
        var source = TestSources.Create(1, 2, 3);

        await foreach (var _ in source.DoAsync(_ => observed.Add(AmbientId.Value)))
        {
        }

        Assert.Equal([42, 42, 42], observed);
    }



    [Fact]
    public async Task ForEachAsync_action_observes_caller_AsyncLocal_value()
    {
        AmbientId.Value = 17;
        var observed = new List<int>();
        var source = TestSources.Create(1, 2, 3);

        await source.ForEachAsync(_ => observed.Add(AmbientId.Value));

        Assert.Equal([17, 17, 17], observed);
    }



    [Fact]
    public async Task DoAsync_async_action_observes_AsyncLocal_across_await_boundary()
    {
        // The async overload awaits Task.Yield inside the action — verify the
        // AsyncLocal value survives the continuation that lands on a different
        // thread-pool worker.
        AmbientId.Value = 99;
        var observedBefore = new List<int>();
        var observedAfter = new List<int>();
        var source = TestSources.Create(1, 2, 3);

        await foreach (var _ in source.DoAsync(async _ =>
        {
            observedBefore.Add(AmbientId.Value);
            await Task.Yield();
            observedAfter.Add(AmbientId.Value);
        }))
        {
        }

        Assert.Equal([99, 99, 99], observedBefore);
        Assert.Equal([99, 99, 99], observedAfter);
    }



    [Fact]
    public async Task DoAsync_AsyncLocal_mutation_inside_action_does_not_leak_to_caller()
    {
        // AsyncLocal mutations inside a sync action ARE visible to the caller
        // (no await boundary to capture the ExecutionContext snapshot), so the
        // last-write-wins. The async overload, by contrast, captures the
        // execution context per await and the inner mutation does NOT leak.
        AmbientId.Value = 1;
        var source = TestSources.Create(1, 2, 3);

        await foreach (var _ in source.DoAsync(async _ =>
        {
            AmbientId.Value = 999;
            await Task.Yield();
        }))
        {
        }

        Assert.Equal(1, AmbientId.Value);  // caller's ambient value preserved across the async-overload pipeline
    }



    // ----------------------------------------------------------------------
    // Property-based / adversarial fuzz tests (deterministic seed)
    //
    // No FsCheck dependency — inline seeded Random gives reproducible runs.
    // ----------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1, 17)]
    [InlineData(1, 1, 23)]
    [InlineData(1, 100, 31)]
    [InlineData(100, 1, 41)]
    [InlineData(100, 7, 43)]
    [InlineData(1000, 13, 53)]
    [InlineData(10_000, 256, 61)]
    public async Task ChunkAsync_property_concatenated_chunks_equal_source(int n, int chunkSize, int seed)
    {
        var rng = new Random(seed);
        var input = new int[n];
        for (var i = 0; i < n; i++)
        {
            input[i] = rng.Next();
        }

        var output = new List<int>();
        await foreach (var chunk in CreateSourceFrom(input).ChunkAsync(chunkSize))
        {
            Assert.True(chunk.Count > 0, "no empty chunks");
            Assert.True(chunk.Count <= chunkSize, $"chunk size {chunk.Count} > max {chunkSize}");
            output.AddRange(chunk);
        }

        Assert.Equal(input, output);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(13)]
    [InlineData(1000)]
    public async Task DoAsync_property_yielded_items_equal_source_and_action_runs_once_per_item(int n)
    {
        var input = Enumerable.Range(0, n).ToArray();
        var actionCalls = new List<int>();
        var output = new List<int>();

        await foreach (var item in CreateSourceFrom(input).DoAsync(actionCalls.Add))
        {
            output.Add(item);
        }

        Assert.Equal(input, output);
        Assert.Equal(input, actionCalls);
    }



    [Theory]
    [InlineData(0, 7)]
    [InlineData(1, 11)]
    [InlineData(13, 19)]
    [InlineData(1000, 29)]
    public async Task NoneAsync_predicate_property_equals_inverse_of_any(int n, int seed)
    {
        // Property: NoneAsync(predicate) <=> source.All(x => !predicate(x))
        var rng = new Random(seed);
        var input = new int[n];
        for (var i = 0; i < n; i++)
        {
            input[i] = rng.Next(0, 100);
        }

        // Random predicate threshold; "match" = element > threshold.
        var threshold = rng.Next(0, 100);
        bool Predicate(int x) => x > threshold;

        var actual = await CreateSourceFrom(input).NoneAsync(Predicate);
        var expected = input.All(x => !Predicate(x));

        Assert.Equal(expected, actual);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    public async Task IsEmptyAsync_property_equals_count_equals_zero(int n)
    {
        var input = Enumerable.Range(0, n).ToArray();

        var actual = await CreateSourceFrom(input).IsEmptyAsync();

        Assert.Equal(n == 0, actual);
    }



    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------

    private static async IAsyncEnumerable<int> CreateSourceFrom(IReadOnlyList<int> values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
        }
    }



    private sealed class DisposeFlag
    {
        public bool Disposed { get; set; }
    }



    /// <summary>
    /// IAsyncEnumerable that yields the natural numbers (0, 1, 2, ...) up to
    /// <paramref name="throwAt"/> (exclusive), then throws <paramref name="exception"/>.
    /// If <c>throwAt</c> is negative, yields indefinitely (cancellation-driven
    /// termination only). Tracks disposal via a shared <see cref="DisposeFlag"/>.
    /// </summary>
    private sealed class ThrowingAsyncEnumerable(DisposeFlag flag, int throwAt, Exception? exception) : IAsyncEnumerable<int>
    {
        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator(flag, throwAt, exception, cancellationToken);

        private sealed class Enumerator(DisposeFlag flag, int throwAt, Exception? exception, CancellationToken ct) : IAsyncEnumerator<int>
        {
            private int _index = -1;

            public int Current { get; private set; }

            public async ValueTask<bool> MoveNextAsync()
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();

                _index++;

                if (throwAt >= 0 && _index >= throwAt && exception is not null)
                {
                    throw exception;
                }

                Current = _index;
                return true;
            }

            public ValueTask DisposeAsync()
            {
                flag.Disposed = true;
                return default;
            }
        }
    }
}
