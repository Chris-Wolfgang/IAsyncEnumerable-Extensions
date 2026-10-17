using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class ScanAsyncTests
{
    [Fact]
    public async Task ScanAsync_when_source_has_items_yields_each_running_value()
    {
        var source = TestSources.Create(1, 2, 3, 4, 5);

        var result = await CollectAsync(source.ScanAsync(0, Sum));

        Assert.Equal([1, 3, 6, 10, 15], result);
    }



    [Fact]
    public async Task ScanAsync_does_not_yield_the_seed()
    {
        var source = TestSources.Create(5);

        var result = await CollectAsync(source.ScanAsync(100, Sum));

        Assert.Equal([105], result);
    }



    [Fact]
    public async Task ScanAsync_when_source_is_empty_yields_nothing()
    {
        var source = TestSources.Create<int>();

        var result = await CollectAsync(source.ScanAsync(0, Sum));

        Assert.Empty(result);
    }



    [Fact]
    public async Task ScanAsync_when_accumulate_type_differs_from_source_type_yields_accumulated_values()
    {
        var source = TestSources.Create(1, 2, 3);

        var result = await CollectAsync(source.ScanAsync("", (text, x) => text + x));

        Assert.Equal(["1", "12", "123"], result);
    }



    [Fact]
    public void ScanAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        var actual = Assert.Throws<ArgumentNullException>(() => source.ScanAsync(0, Sum));

        Assert.Equal("source", actual.ParamName);
    }



    [Fact]
    public void ScanAsync_when_accumulator_is_null_throws_ArgumentNullException()
    {
        var source = TestSources.Create(1, 2, 3);

        var actual = Assert.Throws<ArgumentNullException>(() => source.ScanAsync<int, int>(0, null!));

        Assert.Equal("accumulator", actual.ParamName);
    }



    [Fact]
    public async Task ScanAsync_does_not_enumerate_source_until_consumed()
    {
        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var scanned = source.ScanAsync(0, Sum);

        Assert.False(source.EnumerationStarted);

        await CollectAsync(scanned);

        Assert.True(source.EnumerationStarted);
    }



    [Fact]
    public async Task ScanAsync_with_pre_canceled_token_never_enumerates_source()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var scanned = source.ScanAsync(0, Sum, tokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await scanned.GetAsyncEnumerator().MoveNextAsync()
        );

        Assert.False(source.EnumerationStarted);
    }



    [Fact]
    public async Task ScanAsync_when_cancellation_requested_during_enumeration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = TestSources.CreateDelayed(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4);

        var scanned = source.ScanAsync(0, Sum, tokenSource.Token);

        await using var enumerator = scanned.GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(1, enumerator.Current);

        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await enumerator.MoveNextAsync()
        );
    }



    [Fact]
    public async Task ScanAsync_when_accumulator_throws_propagates_exception_after_earlier_values()
    {
        var source = TestSources.Create(1, 2, 3);
        var sentinel = new InvalidOperationException("test");
        var observed = new List<int>();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => CollectIntoAsync(source.ScanAsync(0, (sum, x) => x == 2 ? throw sentinel : sum + x), observed)
        );

        Assert.Same(sentinel, actual);
        Assert.Equal([1], observed);
    }



    private static int Sum(int sum, int item)
    {
        return sum + item;
    }



    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var result = new List<T>();
        await CollectIntoAsync(source, result);

        return result;
    }



    private static async Task CollectIntoAsync<T>(IAsyncEnumerable<T> source, List<T> sink)
    {
        await foreach (var item in source)
        {
            sink.Add(item);
        }
    }
}
