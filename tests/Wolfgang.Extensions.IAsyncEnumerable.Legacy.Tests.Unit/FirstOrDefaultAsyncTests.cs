namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

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
        var source = TestSources.Create<int>();

        var result = await source.FirstOrDefaultAsync();

        Assert.Equal(0, result);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_when_source_is_empty_reference_type_returns_null()
    {
        var source = TestSources.Create<string>();

        var result = await source.FirstOrDefaultAsync();

        Assert.Null(result);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_when_source_is_empty_nullable_value_type_returns_null()
    {
        var source = TestSources.Create<int?>();

        var result = await source.FirstOrDefaultAsync();

        Assert.Null(result);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_when_source_has_items_returns_first_item()
    {
        var source = TestSources.Create(5, 6, 7);

        var result = await source.FirstOrDefaultAsync();

        Assert.Equal(5, result);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_only_enumerates_first_item()
    {
        var enumerated = new List<int>();
        var source = TestSources.CreateTracking(enumerated, 1, 2, 3);

        await source.FirstOrDefaultAsync();

        Assert.Equal([1], enumerated);
    }



    [Fact]
    public async Task FirstOrDefaultAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.FirstOrDefaultAsync(tokenSource.Token).AsTask()
        );
    }

}
