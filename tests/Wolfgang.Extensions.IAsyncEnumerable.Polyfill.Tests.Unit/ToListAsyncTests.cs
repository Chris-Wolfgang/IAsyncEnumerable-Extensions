namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

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
        var source = CreateSource();

        var result = await source.ToListAsync();

        Assert.Empty(result);
    }



    [Fact]
    public async Task ToListAsync_when_source_has_items_returns_items_in_order()
    {
        var source = CreateSource(1, 2, 3, 4);

        var result = await source.ToListAsync();

        Assert.Equal([1, 2, 3, 4], result);
    }



    [Fact]
    public async Task ToListAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.ToListAsync(tokenSource.Token).AsTask()
        );
    }



    [Fact]
    public async Task ToListAsync_when_token_canceled_mid_iteration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        var source = CreateCancelingSource(tokenSource, 1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.ToListAsync(tokenSource.Token).AsTask()
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



    private static async IAsyncEnumerable<int> CreateCancelingSource(CancellationTokenSource tokenSource, params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
            tokenSource.Cancel();
        }
    }

}
