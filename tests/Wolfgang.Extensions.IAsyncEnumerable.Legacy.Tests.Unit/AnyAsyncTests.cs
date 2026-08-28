namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

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
        var source = TestSources.Create<int>();

        var result = await source.AnyAsync();

        Assert.False(result);
    }



    [Fact]
    public async Task AnyAsync_when_source_has_items_returns_true()
    {
        var source = TestSources.Create(1, 2, 3);

        var result = await source.AnyAsync();

        Assert.True(result);
    }



    [Fact]
    public async Task AnyAsync_only_enumerates_first_item()
    {
        var enumerated = new List<int>();
        var source = TestSources.CreateTracking(enumerated, 1, 2, 3, 4, 5);

        var result = await source.AnyAsync();

        Assert.True(result);
        Assert.Equal([1], enumerated);
    }



    [Fact]
    public async Task AnyAsync_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = TestSources.Create(1, 2, 3);

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
        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.AnyAsync((Func<int, bool>)null!).AsTask()
        );
    }



    [Theory]
    [InlineData(new[] { 1, 2, 3, 4 }, true)]
    [InlineData(new[] { 1, 2, 4, 5 }, false)]
    [InlineData(new int[0], false)]
    public async Task AnyAsync_predicate_match_cases_return_expected(int[] values, bool expected)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var source = TestSources.Create(values);

        var result = await source.AnyAsync(n => n % 3 == 0);

        Assert.Equal(expected, result);
    }



    [Fact]
    public async Task AnyAsync_predicate_short_circuits_on_first_match()
    {
        var enumerated = new List<int>();
        var source = TestSources.CreateTracking(enumerated, 1, 2, 3, 4, 5);

        var result = await source.AnyAsync(n => n == 2);

        Assert.True(result);
        Assert.Equal([1, 2], enumerated);
    }



    [Fact]
    public async Task AnyAsync_predicate_when_predicate_throws_exception_propagates()
    {
        var source = TestSources.Create(1, 2, 3);
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

        var source = TestSources.Create(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.AnyAsync(_ => true, tokenSource.Token).AsTask()
        );
    }



    [Fact]
    public async Task AnyAsync_predicate_when_token_canceled_mid_iteration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        var source = TestSources.Create(1, 2, 3);

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

}
