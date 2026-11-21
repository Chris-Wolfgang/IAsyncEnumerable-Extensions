using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class SlidingWindowAsyncTests
{
    [Fact]
    public async Task SlidingWindowAsync_yields_each_full_window_oldest_first()
    {
        var source = TestSources.Create(1, 2, 3, 4, 5);

        var windows = await CollectAsync(source.SlidingWindowAsync(3));

        Assert.Equal(3, windows.Count);
        Assert.Equal([1, 2, 3], windows[0]);
        Assert.Equal([2, 3, 4], windows[1]);
        Assert.Equal([3, 4, 5], windows[2]);
    }



    [Fact]
    public async Task SlidingWindowAsync_keeps_order_after_the_buffer_wraps_around()
    {
        var source = TestSources.Create(1, 2, 3, 4, 5, 6, 7);

        var windows = await CollectAsync(source.SlidingWindowAsync(3));

        Assert.Equal(5, windows.Count);
        Assert.Equal([4, 5, 6], windows[3]);
        Assert.Equal([5, 6, 7], windows[4]);
    }



    [Fact]
    public async Task SlidingWindowAsync_when_window_size_is_one_yields_each_element_alone()
    {
        var source = TestSources.Create(4, 5, 6);

        var windows = await CollectAsync(source.SlidingWindowAsync(1));

        Assert.Equal(3, windows.Count);
        Assert.Equal([4], windows[0]);
        Assert.Equal([5], windows[1]);
        Assert.Equal([6], windows[2]);
    }



    [Fact]
    public async Task SlidingWindowAsync_when_window_size_equals_source_length_yields_one_window()
    {
        var source = TestSources.Create(1, 2, 3);

        var windows = await CollectAsync(source.SlidingWindowAsync(3));

        Assert.Single(windows);
        Assert.Equal([1, 2, 3], windows[0]);
    }



    [Fact]
    public async Task SlidingWindowAsync_when_source_is_shorter_than_the_window_yields_nothing()
    {
        var source = TestSources.Create(1, 2);

        var windows = await CollectAsync(source.SlidingWindowAsync(3));

        Assert.Empty(windows);
    }



    [Fact]
    public async Task SlidingWindowAsync_when_source_is_empty_yields_nothing()
    {
        var source = TestSources.Create<int>();

        var windows = await CollectAsync(source.SlidingWindowAsync(2));

        Assert.Empty(windows);
    }



    [Fact]
    public async Task SlidingWindowAsync_yields_a_distinct_list_per_window()
    {
        var source = TestSources.Create(1, 2, 3, 4);

        var windows = await CollectAsync(source.SlidingWindowAsync(2));

        Assert.NotSame(windows[0], windows[1]);
        Assert.NotSame(windows[1], windows[2]);

        // Earlier windows are unchanged by the later ones the buffer produced.
        Assert.Equal([1, 2], windows[0]);
        Assert.Equal([2, 3], windows[1]);
        Assert.Equal([3, 4], windows[2]);
    }



    [Fact]
    public void SlidingWindowAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        var actual = Assert.Throws<ArgumentNullException>(() => source.SlidingWindowAsync(2));

        Assert.Equal("source", actual.ParamName);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SlidingWindowAsync_when_window_size_is_less_than_one_throws_ArgumentOutOfRangeException(int windowSize)
    {
        var source = TestSources.Create(1, 2, 3);

        var actual = Assert.Throws<ArgumentOutOfRangeException>(() => source.SlidingWindowAsync(windowSize));

        Assert.Equal("windowSize", actual.ParamName);
    }



    [Fact]
    public async Task SlidingWindowAsync_does_not_enumerate_source_until_consumed()
    {
        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var windows = source.SlidingWindowAsync(2);

        Assert.False(source.EnumerationStarted);

        await CollectAsync(windows);

        Assert.True(source.EnumerationStarted);
    }



    [Fact]
    public async Task SlidingWindowAsync_with_pre_canceled_token_never_enumerates_source()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var windows = source.SlidingWindowAsync(2, tokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await windows.GetAsyncEnumerator().MoveNextAsync()
        );

        Assert.False(source.EnumerationStarted);
    }



    [Fact]
    public async Task SlidingWindowAsync_when_cancellation_requested_during_enumeration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = TestSources.CreateDelayed(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4);

        await using var enumerator = source.SlidingWindowAsync(2, tokenSource.Token).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal([1, 2], enumerator.Current);

        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await enumerator.MoveNextAsync()
        );
    }



    [Fact]
    public async Task SlidingWindowAsync_when_window_size_is_int_MaxValue_yields_nothing_for_a_short_source()
    {
        // The buffer grows with the source, so a huge window does not allocate
        // windowSize elements up front.
        var source = TestSources.Create(1, 2, 3);

        var windows = await CollectAsync(source.SlidingWindowAsync(int.MaxValue));

        Assert.Empty(windows);
    }



    [Fact]
    public async Task SlidingWindowAsync_when_window_outgrows_the_initial_buffer_yields_expected_windows()
    {
        var source = TestSources.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

        var windows = await CollectAsync(source.SlidingWindowAsync(10));

        Assert.Equal(3, windows.Count);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], windows[0]);
        Assert.Equal([2, 3, 4, 5, 6, 7, 8, 9, 10, 11], windows[1]);
        Assert.Equal([3, 4, 5, 6, 7, 8, 9, 10, 11, 12], windows[2]);
    }



    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task SlidingWindowAsync_with_initialCapacity_yields_the_same_windows_as_the_default(int initialCapacity)
    {
        var source = TestSources.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

        var windows = await CollectAsync(source.SlidingWindowAsync(10, initialCapacity));

        Assert.Equal(3, windows.Count);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], windows[0]);
        Assert.Equal([2, 3, 4, 5, 6, 7, 8, 9, 10, 11], windows[1]);
        Assert.Equal([3, 4, 5, 6, 7, 8, 9, 10, 11, 12], windows[2]);
    }



    [Fact]
    public async Task SlidingWindowAsync_with_initialCapacity_when_source_is_shorter_than_the_window_yields_nothing()
    {
        var source = TestSources.Create(1, 2, 3);

        var windows = await CollectAsync(source.SlidingWindowAsync(int.MaxValue, 1_000));

        Assert.Empty(windows);
    }



    [Fact]
    public void SlidingWindowAsync_with_initialCapacity_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        var actual = Assert.Throws<ArgumentNullException>(() => source.SlidingWindowAsync(3, 3));

        Assert.Equal("source", actual.ParamName);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SlidingWindowAsync_with_initialCapacity_when_window_size_is_less_than_one_throws_ArgumentOutOfRangeException(int windowSize)
    {
        var source = TestSources.Create(1, 2, 3);

        var actual = Assert.Throws<ArgumentOutOfRangeException>(() => source.SlidingWindowAsync(windowSize, 1));

        Assert.Equal("windowSize", actual.ParamName);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public void SlidingWindowAsync_when_initialCapacity_is_outside_one_to_windowSize_throws_ArgumentOutOfRangeException(int initialCapacity)
    {
        var source = TestSources.Create(1, 2, 3);

        // Validation is eager: the call itself throws, before any enumeration.
        var actual = Assert.Throws<ArgumentOutOfRangeException>(() => source.SlidingWindowAsync(3, initialCapacity));

        Assert.Equal("initialCapacity", actual.ParamName);
    }



    [Fact]
    public async Task SlidingWindowAsync_with_initialCapacity_when_canceled_during_enumeration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        var source = TestSources.Create(1, 2, 3, 4);

        await using var enumerator = source.SlidingWindowAsync(2, 2, tokenSource.Token).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());

        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await enumerator.MoveNextAsync()
        );
    }



    private static async Task<List<IReadOnlyList<T>>> CollectAsync<T>(IAsyncEnumerable<IReadOnlyList<T>> source)
    {
        var result = new List<IReadOnlyList<T>>();
        await foreach (var window in source)
        {
            result.Add(window);
        }

        return result;
    }
}
