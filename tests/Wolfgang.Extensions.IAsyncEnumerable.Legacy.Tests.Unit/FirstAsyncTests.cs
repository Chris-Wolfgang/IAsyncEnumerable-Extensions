namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

public sealed class FirstAsyncTests
{
    [Fact]
    public async Task FirstAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.FirstAsync().AsTask()
        );
    }



    [Fact]
    public async Task FirstAsync_when_source_is_empty_throws_InvalidOperationException()
    {
        var source = TestSources.Create<int>();

        await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => source.FirstAsync().AsTask()
        );
    }



    [Fact]
    public async Task FirstAsync_when_source_has_items_returns_first_item()
    {
        var source = TestSources.Create(5, 6, 7);

        var result = await source.FirstAsync();

        Assert.Equal(5, result);
    }



    [Fact]
    public async Task FirstAsync_only_enumerates_first_item()
    {
        var enumerated = new List<int>();
        var source = TestSources.CreateTracking(enumerated, 1, 2, 3);

        await source.FirstAsync();

        Assert.Equal([1], enumerated);
    }



    [Fact]
    public async Task FirstAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.FirstAsync(tokenSource.Token).AsTask()
        );
    }

}
