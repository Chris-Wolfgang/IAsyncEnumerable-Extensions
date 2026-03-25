namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class IsNullOrEmptyAsyncTests
{
    [Fact]
    public async Task IsNullOrEmptyAsync_when_source_is_null_returns_true()
    {
        IAsyncEnumerable<int> source = null!;

        var result = await source.IsNullOrEmptyAsync();

        Assert.True(result);
    }



    [Fact]
    public async Task IsNullOrEmptyAsync_when_source_is_empty_returns_true()
    {
        var source = CreateSource();

        var result = await source.IsNullOrEmptyAsync();

        Assert.True(result);
    }



    [Fact]
    public async Task IsNullOrEmptyAsync_when_source_has_items_returns_false()
    {
        var source = CreateSource(1, 2, 3);

        var result = await source.IsNullOrEmptyAsync();

        Assert.False(result);
    }



    [Fact]
    public async Task IsNullOrEmptyAsync_when_source_has_single_item_returns_false()
    {
        var source = CreateSource(42);

        var result = await source.IsNullOrEmptyAsync();

        Assert.False(result);
    }



    [Fact]
    public async Task IsNullOrEmptyAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.IsNullOrEmptyAsync(tokenSource.Token)
        );
    }



    private static async IAsyncEnumerable<int> CreateSource(params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
        }
    }

}
