namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class IsEmptyAsyncTests
{
    [Fact]
    public async Task IsEmptyAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.IsEmptyAsync()
        );
    }



    [Fact]
    public async Task IsEmptyAsync_when_source_is_empty_returns_true()
    {
        var source = TestSources.Create<int>();

        var result = await source.IsEmptyAsync();

        Assert.True(result);
    }



    [Fact]
    public async Task IsEmptyAsync_when_source_has_items_returns_false()
    {
        var source = TestSources.Create(1, 2, 3);

        var result = await source.IsEmptyAsync();

        Assert.False(result);
    }



    [Fact]
    public async Task IsEmptyAsync_when_source_has_single_item_returns_false()
    {
        var source = TestSources.Create(42);

        var result = await source.IsEmptyAsync();

        Assert.False(result);
    }



    [Fact]
    public async Task IsEmptyAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.IsEmptyAsync(tokenSource.Token)
        );
    }



}
