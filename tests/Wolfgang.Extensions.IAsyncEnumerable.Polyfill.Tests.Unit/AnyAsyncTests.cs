namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class AnyAsyncTests
{
    [Fact]
    public async Task AnyAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.AnyAsync().AsTask()
        );
    }



    [Fact]
    public async Task AnyAsync_when_source_is_empty_returns_false()
    {
        var source = CreateSource();

        var result = await source.AnyAsync();

        Assert.False(result);
    }



    [Fact]
    public async Task AnyAsync_when_source_has_items_returns_true()
    {
        var source = CreateSource(1, 2, 3);

        var result = await source.AnyAsync();

        Assert.True(result);
    }



    [Fact]
    public async Task AnyAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.AnyAsync(tokenSource.Token).AsTask()
        );
    }



    [Fact]
    public async Task AnyAsync_predicate_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.AnyAsync(_ => true).AsTask()
        );
    }



    [Fact]
    public async Task AnyAsync_predicate_when_predicate_is_null_throws_ArgumentNullException()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.AnyAsync((Func<int, bool>)null!).AsTask()
        );
    }



    [Fact]
    public async Task AnyAsync_predicate_when_source_is_empty_returns_false()
    {
        var source = CreateSource();

        var result = await source.AnyAsync(n => n % 3 == 0);

        Assert.False(result);
    }



    [Fact]
    public async Task AnyAsync_predicate_when_some_items_match_returns_true()
    {
        var source = CreateSource(1, 2, 3, 4);

        var result = await source.AnyAsync(n => n % 3 == 0);

        Assert.True(result);
    }



    [Fact]
    public async Task AnyAsync_predicate_when_no_items_match_returns_false()
    {
        var source = CreateSource(1, 2, 4, 5);

        var result = await source.AnyAsync(n => n % 3 == 0);

        Assert.False(result);
    }



    [Fact]
    public async Task AnyAsync_predicate_short_circuits_on_first_match()
    {
        var enumerated = new List<int>();
        var source = CreateTrackingSource(enumerated, 1, 2, 3, 4, 5);

        var result = await source.AnyAsync(n => n == 2);

        Assert.True(result);
        Assert.Equal([1, 2], enumerated);
    }



    [Fact]
    public async Task AnyAsync_predicate_when_predicate_throws_exception_propagates()
    {
        var source = CreateSource(1, 2, 3);
        var sentinel = new InvalidOperationException("predicate boom");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => source.AnyAsync(_ => throw sentinel).AsTask()
        );

        Assert.Same(sentinel, actual);
    }



    [Fact]
    public async Task AnyAsync_predicate_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.AnyAsync(_ => true, tokenSource.Token).AsTask()
        );
    }



    [Fact]
    public async Task AnyAsync_predicate_when_token_canceled_mid_iteration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.AnyAsync
            (
                _ =>
                {
                    tokenSource.Cancel();
                    return false;  // don't short-circuit — let the post-predicate token check fire
                },
                tokenSource.Token
            ).AsTask()
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
