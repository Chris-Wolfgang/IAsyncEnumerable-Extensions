namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class ChunkAsyncTests
{
    [Fact]
    public async Task ChunkAsync_when_chunk_size_is_exact_multiple_returns_expected_chunks()
    {
        var source = TestSources.Create(1, 2, 3, 4);

        var chunks = await CollectChunksAsync(source.ChunkAsync(2));

        Assert.Equal(2, chunks.Count);
        Assert.Equal([1, 2], chunks[0]);
        Assert.Equal([3, 4], chunks[1]);
    }

    [Fact]
    public async Task ChunkAsync_when_source_has_remainder_returns_final_partial_chunk()
    {
        var source = TestSources.Create(1, 2, 3, 4, 5);

        var chunks = await CollectChunksAsync(source.ChunkAsync(2));

        Assert.Equal(3, chunks.Count);
        Assert.Equal([1, 2], chunks[0]);
        Assert.Equal([3, 4], chunks[1]);
        Assert.Equal([5], chunks[2]);
    }

    [Fact]
    public async Task ChunkAsync_when_chunk_size_larger_than_source_yields_single_chunk()
    {
        var source = TestSources.Create(1, 2, 3);

        var chunks = await CollectChunksAsync(source.ChunkAsync(10));

        Assert.Single(chunks);
        Assert.Equal([1, 2, 3], chunks[0]);
    }

    [Fact]
    public async Task ChunkAsync_when_chunk_size_is_one_emits_singleton_chunks()
    {
        var source = TestSources.Create(4, 5, 6);

        var chunks = await CollectChunksAsync(source.ChunkAsync(1));

        Assert.Equal(3, chunks.Count);
        Assert.Equal([4], chunks[0]);
        Assert.Equal([5], chunks[1]);
        Assert.Equal([6], chunks[2]);
    }

    [Fact]
    public async Task ChunkAsync_when_source_is_empty_yields_no_chunks()
    {
        var source = TestSources.Create<int>();

        var chunks = await CollectChunksAsync(source.ChunkAsync(4));

        Assert.Empty(chunks);
    }

    [Fact]
    public void ChunkAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        Assert.Throws<ArgumentNullException>(() => source.ChunkAsync(2));
    }

    [Fact]
    public async Task ChunkAsync_yields_distinct_chunk_instances()
    {
        var source = TestSources.Create(1, 2, 3, 4);

        var chunks = await CollectChunksAsync(source.ChunkAsync(2));

        Assert.Equal([1, 2], chunks[0]);
        Assert.Equal([3, 4], chunks[1]);
        Assert.NotSame(chunks[0], chunks[1]);
    }

    [Fact]
    public async Task ChunkAsync_with_delayed_source_preserves_ordering()
    {
        var source = TestSources.CreateDelayed(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4, 5);

        var chunks = await CollectChunksAsync(source.ChunkAsync(3));

        Assert.Equal(2, chunks.Count);
        Assert.Equal([1, 2, 3], chunks[0]);
        Assert.Equal([4, 5], chunks[1]);
    }

    [Fact]
    public async Task ChunkAsync_does_not_enumerate_source_until_consumed()
    {
        var source = new TrackingAsyncEnumerable(1, 2, 3, 4);

        var chunked = source.ChunkAsync(2);

        Assert.False(source.EnumerationStarted);

        await CollectChunksAsync(chunked);

        Assert.True(source.EnumerationStarted);
    }

    [Fact]
    public async Task ChunkAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => CollectChunksAsync(source.ChunkAsync(2, tokenSource.Token))
        );
    }

    [Fact]
    public async Task ChunkAsync_with_pre_canceled_token_never_enumerates_source()
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
    public async Task ChunkAsync_when_cancellation_requested_during_enumeration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = TestSources.CreateDelayed(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4);

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
    public async Task ChunkAsync_when_chunk_size_is_not_positive_throws_ArgumentOutOfRangeException(int chunkSize)
    {
        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CollectChunksAsync(source.ChunkAsync(chunkSize)));
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
}
