namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class FirstOrDefaultAsyncTests
{
    [Fact]
    public async Task FirstOrDefaultAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.FirstOrDefaultAsync().AsTask()
        );
    }



    [Fact]
    public async Task FirstOrDefaultAsync_when_source_is_empty_returns_default()
    {
        var source = CreateSource<int>();

        var result = await source.FirstOrDefaultAsync();

        Assert.Equal(0, result);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_when_source_is_empty_reference_type_returns_null()
    {
        var source = CreateSource<string>();

        var result = await source.FirstOrDefaultAsync();

        Assert.Null(result);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_when_source_has_items_returns_first_item()
    {
        var source = CreateSource(5, 6, 7);

        var result = await source.FirstOrDefaultAsync();

        Assert.Equal(5, result);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_only_enumerates_first_item()
    {
        var enumerated = new List<int>();
        var source = CreateTrackingSource(enumerated, 1, 2, 3);

        await source.FirstOrDefaultAsync();

        Assert.Equal([1], enumerated);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.FirstOrDefaultAsync(tokenSource.Token).AsTask()
        );
    }



    private static async IAsyncEnumerable<T> CreateSource<T>(params T[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
        }
    }



    private static async IAsyncEnumerable<int> CreateTrackingSource(List<int> tracker, params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            tracker.Add(value);
            yield return value;
        }
    }

}
