// Deep-behavior tests for the Legacy terminal operators, modeled on the main
// package's DeepBehaviorTests. Each test pins a contract the per-method test
// files don't exercise directly: the token handed to GetAsyncEnumerator and
// enumerator disposal on every exit path (exception, empty source, cancellation).

namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

public sealed class DeepBehaviorTests
{
    [Fact]
    public async Task CountAsync_passes_token_to_GetAsyncEnumerator()
    {
        using var cts = new CancellationTokenSource();
        var fake = new InstrumentedAsyncEnumerable<int>(1, 2, 3);

        var count = await fake.CountAsync(cts.Token);

        Assert.Equal(3, count);
        Assert.Equal(cts.Token, fake.CapturedToken);
    }



    [Fact]
    public async Task ToListAsync_passes_token_to_GetAsyncEnumerator()
    {
        using var cts = new CancellationTokenSource();
        var fake = new InstrumentedAsyncEnumerable<int>(1, 2, 3);

        var list = await fake.ToListAsync(cts.Token);

        Assert.Equal([1, 2, 3], list);
        Assert.Equal(cts.Token, fake.CapturedToken);
    }



    [Fact]
    public async Task AnyAsync_predicate_when_predicate_throws_disposes_enumerator()
    {
        var sentinel = new InvalidOperationException("predicate boom");
        var fake = new InstrumentedAsyncEnumerable<int>(1, 2, 3);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => fake.AnyAsync(_ => throw sentinel).AsTask()
        );

        Assert.Same(sentinel, actual);
        Assert.True(fake.Disposed, "predicate exception must not skip enumerator disposal");
    }



    [Fact]
    public async Task FirstAsync_when_source_is_empty_disposes_enumerator()
    {
        var fake = new InstrumentedAsyncEnumerable<int>();

        await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => fake.FirstAsync().AsTask()
        );

        Assert.True(fake.Disposed, "empty-source InvalidOperationException must not skip enumerator disposal");
    }



    [Fact]
    public async Task CountAsync_when_canceled_mid_iteration_disposes_enumerator()
    {
        // The first yielded item cancels the token; the post-increment
        // ThrowIfCancellationRequested in the core loop throws, and the
        // await-using must still dispose the enumerator.
        using var cts = new CancellationTokenSource();
        var fake = new InstrumentedAsyncEnumerable<int>(1, 2, 3)
        {
            OnItemYielded = () => cts.Cancel()
        };

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => fake.CountAsync(cts.Token).AsTask()
        );

        Assert.True(fake.Disposed, "mid-iteration cancellation must dispose the source enumerator");
    }



    /// <summary>
    /// IAsyncEnumerable fake that yields a fixed set of values while recording
    /// the CancellationToken passed to <see cref="GetAsyncEnumerator"/> and
    /// whether its enumerator was disposed. An optional
    /// <see cref="OnItemYielded"/> callback runs after each item is produced,
    /// letting tests cancel a token mid-iteration.
    /// </summary>
    private sealed class InstrumentedAsyncEnumerable<T>(params T[] values) : IAsyncEnumerable<T>
    {
        public CancellationToken CapturedToken { get; private set; }

        public bool Disposed { get; private set; }

        public Action? OnItemYielded { get; set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            CapturedToken = cancellationToken;
            return new Enumerator(this, values);
        }

        private sealed class Enumerator(InstrumentedAsyncEnumerable<T> owner, T[] values) : IAsyncEnumerator<T>
        {
            private int _index = -1;

            public T Current { get; private set; } = default!;

            public async ValueTask<bool> MoveNextAsync()
            {
                await Task.Yield();

                _index++;
                if (_index >= values.Length)
                {
                    return false;
                }

                Current = values[_index];
                owner.OnItemYielded?.Invoke();
                return true;
            }

            public ValueTask DisposeAsync()
            {
                owner.Disposed = true;
                return default;
            }
        }
    }
}
