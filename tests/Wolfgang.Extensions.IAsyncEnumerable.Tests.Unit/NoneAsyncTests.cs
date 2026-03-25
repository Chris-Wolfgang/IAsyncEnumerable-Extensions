namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class NoneAsyncTests
{
    [Fact]
    public async Task NoneAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.NoneAsync()
        );
    }



    [Fact]
    public async Task NoneAsync_when_source_is_empty_returns_true()
    {
        var source = CreateSource();

        var result = await source.NoneAsync();

        Assert.True(result);
    }



    [Fact]
    public async Task NoneAsync_when_source_has_items_returns_false()
    {
        var source = CreateSource(1, 2, 3);

        var result = await source.NoneAsync();

        Assert.False(result);
    }



    [Fact]
    public async Task NoneAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.NoneAsync(tokenSource.Token)
        );
    }



    [Fact]
    public async Task NoneAsync_predicate_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.NoneAsync(_ => true)
        );
    }



    [Fact]
    public async Task NoneAsync_predicate_when_predicate_is_null_throws_ArgumentNullException()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.NoneAsync((Func<int, bool>)null!)
        );
    }



    [Fact]
    public async Task NoneAsync_predicate_when_all_items_match_returns_false()
    {
        var source = CreateSource(3, 6, 9, 12);

        var result = await source.NoneAsync(n => n % 3 == 0);

        Assert.False(result);
    }



    [Fact]
    public async Task NoneAsync_predicate_when_some_items_match_returns_false()
    {
        var source = CreateSource(1, 2, 3, 4);

        var result = await source.NoneAsync(n => n % 3 == 0);

        Assert.False(result);
    }



    [Fact]
    public async Task NoneAsync_predicate_when_no_items_match_returns_true()
    {
        var source = CreateSource(1, 2, 4, 5);

        var result = await source.NoneAsync(n => n % 3 == 0);

        Assert.True(result);
    }



    [Fact]
    public async Task NoneAsync_predicate_when_source_is_empty_returns_true()
    {
        var source = CreateSource();

        var result = await source.NoneAsync(n => n % 3 == 0);

        Assert.True(result);
    }



    [Fact]
    public async Task NoneAsync_predicate_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.NoneAsync(_ => true, tokenSource.Token)
        );
    }



    [Fact]
    public async Task NoneAsync_predicate_short_circuits_on_first_match()
    {
        var enumerated = new List<int>();
        var source = CreateTrackingSource(enumerated, 1, 2, 3, 4, 5);

        var result = await source.NoneAsync(n => n == 2);

        Assert.False(result);
        Assert.Equal([1, 2], enumerated);
    }



    private static async IAsyncEnumerable<int> CreateSource(params int[] values)
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
