using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class DistinctUntilChangedAsyncTests
{
    [Fact]
    public async Task DistinctUntilChangedAsync_when_source_has_adjacent_duplicates_collapses_each_run()
    {
        var source = TestSources.Create(1, 1, 2, 2, 2, 3, 1, 1);

        var result = await CollectAsync(source.DistinctUntilChangedAsync());

        Assert.Equal([1, 2, 3, 1], result);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_when_source_has_no_adjacent_duplicates_yields_every_element()
    {
        var source = TestSources.Create(1, 2, 1, 2);

        var result = await CollectAsync(source.DistinctUntilChangedAsync());

        Assert.Equal([1, 2, 1, 2], result);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_when_every_element_is_equal_yields_one()
    {
        var source = TestSources.Create(7, 7, 7, 7);

        var result = await CollectAsync(source.DistinctUntilChangedAsync());

        Assert.Equal([7], result);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_when_source_has_one_element_yields_it()
    {
        var source = TestSources.Create(42);

        var result = await CollectAsync(source.DistinctUntilChangedAsync());

        Assert.Equal([42], result);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_when_source_is_empty_yields_nothing()
    {
        var source = TestSources.Create<int>();

        var result = await CollectAsync(source.DistinctUntilChangedAsync());

        Assert.Empty(result);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_with_comparer_uses_it_and_keeps_the_first_of_each_run()
    {
        var source = TestSources.Create("a", "A", "b", "B", "a");

        var result = await CollectAsync(source.DistinctUntilChangedAsync(StringComparer.OrdinalIgnoreCase));

        Assert.Equal(["a", "b", "a"], result);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_with_null_comparer_uses_default_equality()
    {
        var source = TestSources.Create("a", "A", "A");

        var result = await CollectAsync(source.DistinctUntilChangedAsync(comparer: null));

        Assert.Equal(["a", "A"], result);
    }



    [Fact]
    public async Task DistinctUntilChangedByAsync_compares_keys_and_yields_first_of_each_run()
    {
        var source = TestSources.Create("1x", "1y", "2z", "1w");

        var result = await CollectAsync(source.DistinctUntilChangedByAsync(KeyOf));

        Assert.Equal(["1x", "2z", "1w"], result);
    }



    [Fact]
    public async Task DistinctUntilChangedByAsync_with_comparer_uses_the_comparer_on_keys()
    {
        var source = TestSources.Create("1Up", "2UP", "3down");

        var result = await CollectAsync(source.DistinctUntilChangedByAsync(NameOf, StringComparer.OrdinalIgnoreCase));

        Assert.Equal(["1Up", "3down"], result);
    }



    [Fact]
    public async Task DistinctUntilChangedByAsync_calls_the_key_selector_once_per_element()
    {
        var source = TestSources.Create(1, 1, 2, 3, 3);
        var counter = new KeyCounter();

        var result = await CollectAsync(source.DistinctUntilChangedByAsync(counter.Key));

        Assert.Equal([1, 2, 3], result);
        Assert.Equal(5, counter.Calls);
    }



    [Fact]
    public void DistinctUntilChangedAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        var actual = Assert.Throws<ArgumentNullException>(() => source.DistinctUntilChangedAsync());

        Assert.Equal("source", actual.ParamName);
    }



    [Fact]
    public void DistinctUntilChangedByAsync_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<string> source = null!;

        var actual = Assert.Throws<ArgumentNullException>(() => source.DistinctUntilChangedByAsync(KeyOf));

        Assert.Equal("source", actual.ParamName);
    }



    [Fact]
    public void DistinctUntilChangedByAsync_when_key_selector_is_null_throws_ArgumentNullException()
    {
        var source = TestSources.Create(1, 2, 3);

        var actual = Assert.Throws<ArgumentNullException>(() => source.DistinctUntilChangedByAsync<int, int>(null!));

        Assert.Equal("keySelector", actual.ParamName);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_does_not_enumerate_source_until_consumed()
    {
        var source = new TrackingAsyncEnumerable(1, 1, 2);

        var distinct = source.DistinctUntilChangedAsync();

        Assert.False(source.EnumerationStarted);

        await CollectAsync(distinct);

        Assert.True(source.EnumerationStarted);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_with_pre_canceled_token_never_enumerates_source()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = new TrackingAsyncEnumerable(1, 1, 2);

        var distinct = source.DistinctUntilChangedAsync(token: tokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await distinct.GetAsyncEnumerator().MoveNextAsync()
        );

        Assert.False(source.EnumerationStarted);
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_when_canceled_after_the_first_element_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = TestSources.CreateDelayed(TimeSpan.FromMilliseconds(10), 1, 2, 3);

        await using var enumerator = source.DistinctUntilChangedAsync(token: tokenSource.Token).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(1, enumerator.Current);

        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await enumerator.MoveNextAsync()
        );
    }



    [Fact]
    public async Task DistinctUntilChangedAsync_when_canceled_after_a_later_element_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = TestSources.CreateDelayed(TimeSpan.FromMilliseconds(10), 1, 2, 3);

        await using var enumerator = source.DistinctUntilChangedAsync(token: tokenSource.Token).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(2, enumerator.Current);

        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await enumerator.MoveNextAsync()
        );
    }



    [Fact]
    public async Task DistinctUntilChangedByAsync_when_key_selector_throws_propagates_exception()
    {
        var source = TestSources.Create(1, 2, 3);
        var thrower = new ThrowingKey(throwOn: 2);

        await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => CollectAsync(source.DistinctUntilChangedByAsync(thrower.Key))
        );
    }



    // Items are "<key digit><name>", e.g. "1x": the key is the leading character.
    private static char KeyOf(string item)
    {
        return item[0];
    }



    private static string NameOf(string item)
    {
        return item.Substring(1);
    }



    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var result = new List<T>();
        await foreach (var item in source)
        {
            result.Add(item);
        }

        return result;
    }



    private sealed class KeyCounter
    {
        public int Calls { get; private set; }



        public int Key(int item)
        {
            Calls++;
            return item;
        }
    }



    private sealed class ThrowingKey(int throwOn)
    {
        public int Key(int item)
        {
            return item == throwOn ? throw new InvalidOperationException("test") : item;
        }
    }
}
