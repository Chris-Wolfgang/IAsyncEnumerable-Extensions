namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class ChunkAsyncTests
{
    [Fact]
    public async Task ChunkAsync_WithExactMultipleChunkSize_ReturnsExpectedChunks()
    {
        var source = CreateSource(1, 2, 3, 4);

        var chunks = await CollectChunksAsync(source.ChunkAsync(2));

        Assert.Equal(2, chunks.Count);
        Assert.Equal([1, 2], chunks[0]);
        Assert.Equal([3, 4], chunks[1]);
    }

    [Fact]
    public async Task ChunkAsync_WithRemainderChunk_ReturnsFinalPartialChunk()
    {
        var source = CreateSource(1, 2, 3, 4, 5);

        var chunks = await CollectChunksAsync(source.ChunkAsync(2));

        Assert.Equal(3, chunks.Count);
        Assert.Equal([1, 2], chunks[0]);
        Assert.Equal([3, 4], chunks[1]);
        Assert.Equal([5], chunks[2]);
    }

    [Fact]
    public async Task ChunkAsync_WithChunkSizeLargerThanSource_YieldsSingleChunk()
    {
        var source = CreateSource(1, 2, 3);

        var chunks = await CollectChunksAsync(source.ChunkAsync(10));

        Assert.Single(chunks);
        Assert.Equal([1, 2, 3], chunks[0]);
    }

    [Fact]
    public async Task ChunkAsync_WithChunkSizeOfOne_EmitsSingletonChunks()
    {
        var source = CreateSource(4, 5, 6);

        var chunks = await CollectChunksAsync(source.ChunkAsync(1));

        Assert.Equal(3, chunks.Count);
        Assert.Equal([4], chunks[0]);
        Assert.Equal([5], chunks[1]);
        Assert.Equal([6], chunks[2]);
    }

    [Fact]
    public async Task ChunkAsync_WithEmptySource_YieldsNoChunks()
    {
        var source = CreateSource();

        var chunks = await CollectChunksAsync(source.ChunkAsync(4));

        Assert.Empty(chunks);
    }

    [Fact]
    public void ChunkAsync_WithNullSource_ThrowsArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        Assert.Throws<ArgumentNullException>(() => source.ChunkAsync(2));
    }

    [Fact]
    public async Task ChunkAsync_YieldsDistinctChunkInstances()
    {
        var source = CreateSource(1, 2, 3, 4);

        var chunks = await CollectChunksAsync(source.ChunkAsync(2));

        Assert.Equal([1, 2], chunks[0]);
        Assert.Equal([3, 4], chunks[1]);
        Assert.NotSame(chunks[0], chunks[1]);
    }

    [Fact]
    public async Task ChunkAsync_WithDelayedSource_PreservesOrdering()
    {
        var source = CreateDelayedSource(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4, 5);

        var chunks = await CollectChunksAsync(source.ChunkAsync(3));

        Assert.Equal(2, chunks.Count);
        Assert.Equal([1, 2, 3], chunks[0]);
        Assert.Equal([4, 5], chunks[1]);
    }

    [Fact]
    public async Task ChunkAsync_DoesNotEnumerateSourceUntilConsumed()
    {
        var source = new TrackingAsyncEnumerable(1, 2, 3, 4);

        var chunked = source.ChunkAsync(2);

        Assert.False(source.EnumerationStarted);

        await CollectChunksAsync(chunked);

        Assert.True(source.EnumerationStarted);
    }

    [Fact]
    public async Task ChunkAsync_WithPreCanceledToken_ThrowsOperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => CollectChunksAsync(source.ChunkAsync(2, tokenSource.Token))
        );
    }

    [Fact]
    public async Task ChunkAsync_WithPreCanceledToken_NeverEnumeratesSource()
    {
        // The pre-loop ThrowIfCancellationRequested() must fire before the source is
        // ever touched. A source with enough items to fill a full chunk would otherwise
        // let the in-loop check (after the first yield) catch the same pre-canceled
        // token, masking a removed pre-loop check.
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var chunked = source.ChunkAsync(2, tokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await chunked.GetAsyncEnumerator().MoveNextAsync()
        );

        Assert.False(source.EnumerationStarted);
    }



    [Fact]
    public async Task ChunkAsync_WhenCancellationRequestedDuringEnumeration_ThrowsOperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = CreateDelayedSource(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4);

        var chunked = source.ChunkAsync(2, tokenSource.Token);

        await using var enumerator = chunked.GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());

        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await enumerator.MoveNextAsync()
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public async Task ChunkAsync_WithNonPositiveChunkSize_ThrowsArgumentOutOfRangeException(int chunkSize)
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CollectChunksAsync(source.ChunkAsync(chunkSize)));
    }

    private static async IAsyncEnumerable<int> CreateSource(params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
        }
    }

    private static async IAsyncEnumerable<int> CreateDelayedSource(TimeSpan delay, params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Delay(delay);
            yield return value;
        }
    }

    private static async Task<List<ICollection<int>>> CollectChunksAsync(IAsyncEnumerable<ICollection<int>> chunks)
    {
        var result = new List<ICollection<int>>();
        await foreach (var chunk in chunks)
        {
            result.Add(chunk);
        }

        return result;
    }

    private sealed class TrackingAsyncEnumerable(params int[] values) : IAsyncEnumerable<int>
    {
        public bool EnumerationStarted { get; private set; }

        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnumerationStarted = true;
            return CreateSource(values).GetAsyncEnumerator(cancellationToken);
        }
    }
}
