namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

public sealed class CountAsyncTests
{
    [Fact]
    public async Task CountAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.CountAsync().AsTask()
        );
    }



    [Fact]
    public async Task CountAsync_when_source_is_empty_returns_zero()
    {
        var source = TestSources.Create<int>();

        var result = await source.CountAsync();

        Assert.Equal(0, result);
    }



    [Fact]
    public async Task CountAsync_when_source_has_one_item_returns_one()
    {
        var source = TestSources.Create(42);

        var result = await source.CountAsync();

        Assert.Equal(1, result);
    }



    [Fact]
    public async Task CountAsync_when_source_has_items_returns_item_count()
    {
        var source = TestSources.Create(1, 2, 3, 4);

        var result = await source.CountAsync();

        Assert.Equal(4, result);
    }



    [Fact]
    public async Task CountAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.CountAsync(tokenSource.Token).AsTask()
        );
    }



    [Fact]
    public async Task CountAsync_when_token_canceled_mid_iteration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        var source = TestSources.CreateCanceling(tokenSource, 1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.CountAsync(tokenSource.Token).AsTask()
        );
    }

}
