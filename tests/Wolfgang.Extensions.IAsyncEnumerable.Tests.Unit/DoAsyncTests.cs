namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class DoAsyncTests
{
    [Fact]
    public async Task DoAsync_Action_when_source_has_items_executes_action_on_each()
    {
        var source = CreateSource(1, 2, 3);
        var observed = new List<int>();

        var result = await CollectAsync(source.DoAsync(x => observed.Add(x)));

        Assert.Equal([1, 2, 3], observed);
        Assert.Equal([1, 2, 3], result);
    }



    [Fact]
    public async Task DoAsync_Action_when_source_is_empty_executes_no_actions()
    {
        var source = CreateSource();
        var observed = new List<int>();

        var result = await CollectAsync(source.DoAsync(x => observed.Add(x)));

        Assert.Empty(observed);
        Assert.Empty(result);
    }



    [Fact]
    public async Task DoAsync_Action_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => CollectAsync(source.DoAsync(_ => { }))
        );
    }



    [Fact]
    public async Task DoAsync_Action_when_action_is_null_throws_ArgumentNullException()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => CollectAsync(source.DoAsync((Action<int>)null!))
        );
    }



    [Fact]
    public async Task DoAsync_Action_yields_original_items_unchanged()
    {
        var source = CreateSource(10, 20, 30);

        var result = await CollectAsync(source.DoAsync(_ => { }));

        Assert.Equal([10, 20, 30], result);
    }



    [Fact]
    public async Task DoAsync_Action_preserves_ordering()
    {
        var source = CreateSource(5, 3, 1, 4, 2);
        var observed = new List<int>();

        var result = await CollectAsync(source.DoAsync(x => observed.Add(x)));

        Assert.Equal([5, 3, 1, 4, 2], observed);
        Assert.Equal([5, 3, 1, 4, 2], result);
    }



    [Fact]
    public async Task DoAsync_Action_does_not_enumerate_source_until_consumed()
    {
        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var tapped = source.DoAsync(_ => { });

        Assert.False(source.EnumerationStarted);

        await CollectAsync(tapped);

        Assert.True(source.EnumerationStarted);
    }



    [Fact]
    public async Task DoAsync_Action_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => CollectAsync(source.DoAsync(_ => { }, tokenSource.Token))
        );
    }



    [Fact]
    public async Task DoAsync_Action_with_pre_canceled_token_never_enumerates_source()
    {
        // The pre-loop ThrowIfCancellationRequested() must fire on the very first
        // MoveNextAsync() call, before the source is touched. Draining the whole
        // sequence would instead hit the in-loop check after the first yield, which
        // fires on this same pre-canceled token regardless of the pre-loop check.
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var tapped = source.DoAsync(_ => { }, tokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await tapped.GetAsyncEnumerator().MoveNextAsync()
        );

        Assert.False(source.EnumerationStarted);
    }



    [Fact]
    public async Task DoAsync_Action_when_cancellation_requested_during_enumeration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = CreateDelayedSource(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4);

        var tapped = source.DoAsync(_ => { }, tokenSource.Token);

        await using var enumerator = tapped.GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());

        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await enumerator.MoveNextAsync()
        );
    }



    [Fact]
    public async Task DoAsync_Action_when_action_throws_propagates_exception()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => CollectAsync(source.DoAsync(x =>
            {
                if (x == 2)
                {
                    throw new InvalidOperationException("test");
                }
            }))
        );
    }



    [Fact]
    public async Task DoAsync_Action_executes_action_before_yielding_item()
    {
        var source = CreateSource(1, 2, 3);
        var log = new List<string>();

        await foreach (var item in source.DoAsync(x => log.Add($"action:{x}")))
        {
            log.Add($"yield:{item}");
        }

        Assert.Equal
        (
            new[] { "action:1", "yield:1", "action:2", "yield:2", "action:3", "yield:3" },
            log
        );
    }



    [Fact]
    public async Task DoAsync_Func_when_source_has_items_executes_async_action_on_each()
    {
        var source = CreateSource(1, 2, 3);
        var observed = new List<int>();

        var result = await CollectAsync(source.DoAsync(async x =>
        {
            await Task.Yield();
            observed.Add(x);
        }));

        Assert.Equal([1, 2, 3], observed);
        Assert.Equal([1, 2, 3], result);
    }



    [Fact]
    public async Task DoAsync_Func_when_source_is_empty_executes_no_actions()
    {
        var source = CreateSource();
        var observed = new List<int>();

        var result = await CollectAsync(source.DoAsync(async x =>
        {
            await Task.Yield();
            observed.Add(x);
        }));

        Assert.Empty(observed);
        Assert.Empty(result);
    }



    [Fact]
    public async Task DoAsync_Func_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => CollectAsync(source.DoAsync(async _ => await Task.Yield()))
        );
    }



    [Fact]
    public async Task DoAsync_Func_when_action_is_null_throws_ArgumentNullException()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => CollectAsync(source.DoAsync((Func<int, Task>)null!))
        );
    }



    [Fact]
    public async Task DoAsync_Func_yields_original_items_unchanged()
    {
        var source = CreateSource(10, 20, 30);

        var result = await CollectAsync(source.DoAsync(_ => Task.CompletedTask));

        Assert.Equal([10, 20, 30], result);
    }



    [Fact]
    public async Task DoAsync_Func_preserves_ordering()
    {
        var source = CreateSource(5, 3, 1, 4, 2);
        var observed = new List<int>();

        var result = await CollectAsync(source.DoAsync(x =>
        {
            observed.Add(x);
            return Task.CompletedTask;
        }));

        Assert.Equal([5, 3, 1, 4, 2], observed);
        Assert.Equal([5, 3, 1, 4, 2], result);
    }



    [Fact]
    public async Task DoAsync_Func_does_not_enumerate_source_until_consumed()
    {
        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var tapped = source.DoAsync(_ => Task.CompletedTask);

        Assert.False(source.EnumerationStarted);

        await CollectAsync(tapped);

        Assert.True(source.EnumerationStarted);
    }



    [Fact]
    public async Task DoAsync_Func_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => CollectAsync(source.DoAsync(_ => Task.CompletedTask, tokenSource.Token))
        );
    }



    [Fact]
    public async Task DoAsync_Func_with_pre_canceled_token_never_enumerates_source()
    {
        // Same isolation as the Action overload's pre-loop check: assert on the very
        // first MoveNextAsync() call rather than draining the sequence, so the
        // in-loop check (after the first yield) can't mask a removed pre-loop check.
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = new TrackingAsyncEnumerable(1, 2, 3);

        var tapped = source.DoAsync(_ => Task.CompletedTask, tokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await tapped.GetAsyncEnumerator().MoveNextAsync()
        );

        Assert.False(source.EnumerationStarted);
    }



    [Fact]
    public async Task DoAsync_Func_when_cancellation_requested_during_enumeration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = CreateDelayedSource(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4);

        var tapped = source.DoAsync(_ => Task.CompletedTask, tokenSource.Token);

        await using var enumerator = tapped.GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());

        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () => await enumerator.MoveNextAsync()
        );
    }



    [Fact]
    public async Task DoAsync_Func_when_action_throws_propagates_exception()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => CollectAsync(source.DoAsync(x =>
            {
                if (x == 2)
                {
                    throw new InvalidOperationException("test");
                }

                return Task.CompletedTask;
            }))
        );
    }



    [Fact]
    public async Task DoAsync_Func_executes_action_before_yielding_item()
    {
        var source = CreateSource(1, 2, 3);
        var log = new List<string>();

        await foreach (var item in source.DoAsync(x =>
        {
            log.Add($"action:{x}");
            return Task.CompletedTask;
        }))
        {
            log.Add($"yield:{item}");
        }

        Assert.Equal
        (
            new[] { "action:1", "yield:1", "action:2", "yield:2", "action:3", "yield:3" },
            log
        );
    }



    [Fact]
    public async Task DoAsync_Action_can_chain_with_ChunkAsync()
    {
        var source = CreateSource(1, 2, 3, 4, 5, 6);
        var observed = new List<int>();

        var chunks = new List<ICollection<int>>();
        await foreach (var chunk in source.DoAsync(x => observed.Add(x)).ChunkAsync(3))
        {
            chunks.Add(chunk);
        }

        Assert.Equal([1, 2, 3, 4, 5, 6], observed);
        Assert.Equal(2, chunks.Count);
        Assert.Equal([1, 2, 3], chunks[0]);
        Assert.Equal([4, 5, 6], chunks[1]);
    }



    [Fact]
    public async Task DoAsync_Func_can_chain_with_ChunkAsync()
    {
        var source = CreateSource(1, 2, 3, 4, 5, 6);
        var observed = new List<int>();

        var chunks = new List<ICollection<int>>();
        await foreach (var chunk in source.DoAsync(x =>
        {
            observed.Add(x);
            return Task.CompletedTask;
        }).ChunkAsync(3))
        {
            chunks.Add(chunk);
        }

        Assert.Equal([1, 2, 3, 4, 5, 6], observed);
        Assert.Equal(2, chunks.Count);
        Assert.Equal([1, 2, 3], chunks[0]);
        Assert.Equal([4, 5, 6], chunks[1]);
    }



    private static async IAsyncEnumerable<int> CreateSource(params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
        }
    }

    private static async IAsyncEnumerable<int> CreateDelayedSource(TimeSpan delay, params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Delay(delay);
            yield return value;
        }
    }

    private static async Task<List<int>> CollectAsync(IAsyncEnumerable<int> source)
    {
        var result = new List<int>();
        await foreach (var item in source)
        {
            result.Add(item);
        }

        return result;
    }

    private sealed class TrackingAsyncEnumerable(params int[] values) : IAsyncEnumerable<int>
    {
        public bool EnumerationStarted { get; private set; }

        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnumerationStarted = true;
            return CreateSource(values).GetAsyncEnumerator(cancellationToken);
        }
    }
}
