namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

public sealed class ToListAsyncTests
{
    [Fact]
    public async Task ToListAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.ToListAsync().AsTask()
        );
    }



    [Fact]
    public async Task ToListAsync_when_source_is_empty_returns_empty_list()
    {
        var source = TestSources.Create<int>();

        var result = await source.ToListAsync();

        Assert.Empty(result);
    }



    [Fact]
    public async Task ToListAsync_when_source_has_one_item_returns_single_item_list()
    {
        var source = TestSources.Create(42);

        var result = await source.ToListAsync();

        Assert.Equal([42], result);
    }



    [Fact]
    public async Task ToListAsync_when_source_has_items_returns_items_in_order()
    {
        var source = TestSources.Create(1, 2, 3, 4);

        var result = await source.ToListAsync();

        Assert.Equal([1, 2, 3, 4], result);
    }



    [Fact]
    public async Task ToListAsync_returns_independent_list_per_call()
    {
        var list1 = await TestSources.Create(1, 2, 3).ToListAsync();
        var list2 = await TestSources.Create(1, 2, 3).ToListAsync();

        Assert.NotSame(list1, list2);
        Assert.Equal([1, 2, 3], list1);
        Assert.Equal([1, 2, 3], list2);
    }



    [Fact]
    public async Task ToListAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.ToListAsync(tokenSource.Token).AsTask()
        );
    }



    [Fact]
    public async Task ToListAsync_when_token_canceled_mid_iteration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        var source = TestSources.CreateCanceling(tokenSource, 1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.ToListAsync(tokenSource.Token).AsTask()
        );
    }

}
