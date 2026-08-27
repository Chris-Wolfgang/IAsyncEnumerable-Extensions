namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

public sealed class ForEachAsyncTests
{
    [Fact]
    public async Task ForEachAsync_Action_when_source_has_items_executes_action_on_each()
    {
        var source = CreateSource(1, 2, 3);
        var observed = new List<int>();

        await source.ForEachAsync(x => observed.Add(x));

        Assert.Equal([1, 2, 3], observed);
    }



    [Fact]
    public async Task ForEachAsync_Action_when_source_is_empty_executes_no_actions()
    {
        var source = CreateSource();
        var observed = new List<int>();

        await source.ForEachAsync(x => observed.Add(x));

        Assert.Empty(observed);
    }



    [Fact]
    public async Task ForEachAsync_Action_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.ForEachAsync(_ => { })
        );
    }



    [Fact]
    public async Task ForEachAsync_Action_when_action_is_null_throws_ArgumentNullException()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.ForEachAsync((Action<int>)null!)
        );
    }



    [Fact]
    public async Task ForEachAsync_Action_preserves_ordering()
    {
        var source = CreateSource(5, 3, 1, 4, 2);
        var observed = new List<int>();

        await source.ForEachAsync(x => observed.Add(x));

        Assert.Equal([5, 3, 1, 4, 2], observed);
    }



    [Fact]
    public async Task ForEachAsync_Action_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.ForEachAsync(_ => { }, tokenSource.Token)
        );
    }



    [Fact]
    public async Task ForEachAsync_Action_with_pre_canceled_token_never_invokes_action()
    {
        // The pre-loop ThrowIfCancellationRequested() must fire before the source is
        // ever enumerated. Asserting only the exception type would still pass if that
        // check were removed, since the in-loop check (after the first action call)
        // fires on this same pre-canceled token — so assert the action never ran too.
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);
        var invoked = false;

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.ForEachAsync(_ => invoked = true, tokenSource.Token)
        );

        Assert.False(invoked);
    }



    [Fact]
    public async Task ForEachAsync_Action_when_cancellation_requested_during_enumeration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = CreateDelayedSource(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4);
        var observed = new List<int>();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () =>
            {
                await source.ForEachAsync
                (
                    x =>
                    {
                        observed.Add(x);
                        if (x == 1)
                        {
                            tokenSource.Cancel();
                        }
                    },
                    tokenSource.Token
                );
            }
        );
    }



    [Fact]
    public async Task ForEachAsync_Action_when_action_throws_propagates_exception()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => source.ForEachAsync(x =>
            {
                if (x == 2)
                {
                    throw new InvalidOperationException("test");
                }
            })
        );
    }



    [Fact]
    public async Task ForEachAsync_Func_when_source_has_items_executes_async_action_on_each()
    {
        var source = CreateSource(1, 2, 3);
        var observed = new List<int>();

        await source.ForEachAsync(async x =>
        {
            await Task.Yield();
            observed.Add(x);
        });

        Assert.Equal([1, 2, 3], observed);
    }



    [Fact]
    public async Task ForEachAsync_Func_when_source_is_empty_executes_no_actions()
    {
        var source = CreateSource();
        var observed = new List<int>();

        await source.ForEachAsync(async x =>
        {
            await Task.Yield();
            observed.Add(x);
        });

        Assert.Empty(observed);
    }



    [Fact]
    public async Task ForEachAsync_Func_when_source_is_null_throws_ArgumentNullException()
    {
        IAsyncEnumerable<int> source = null!;

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.ForEachAsync(async _ => await Task.Yield())
        );
    }



    [Fact]
    public async Task ForEachAsync_Func_when_action_is_null_throws_ArgumentNullException()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<ArgumentNullException>
        (
            () => source.ForEachAsync((Func<int, Task>)null!)
        );
    }



    [Fact]
    public async Task ForEachAsync_Func_preserves_ordering()
    {
        var source = CreateSource(5, 3, 1, 4, 2);
        var observed = new List<int>();

        await source.ForEachAsync(x =>
        {
            observed.Add(x);
            return Task.CompletedTask;
        });

        Assert.Equal([5, 3, 1, 4, 2], observed);
    }



    [Fact]
    public async Task ForEachAsync_Func_with_pre_canceled_token_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.ForEachAsync(_ => Task.CompletedTask, tokenSource.Token)
        );
    }



    [Fact]
    public async Task ForEachAsync_Func_with_pre_canceled_token_never_invokes_action()
    {
        // Same isolation as the Action overload's pre-loop check — assert the async
        // action never ran, not just that some OperationCanceledException surfaced.
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var source = CreateSource(1, 2, 3);
        var invoked = false;

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            () => source.ForEachAsync(_ =>
            {
                invoked = true;
                return Task.CompletedTask;
            }, tokenSource.Token)
        );

        Assert.False(invoked);
    }



    [Fact]
    public async Task ForEachAsync_Func_when_cancellation_requested_during_enumeration_throws_OperationCanceledException()
    {
        using var tokenSource = new CancellationTokenSource();

        var source = CreateDelayedSource(TimeSpan.FromMilliseconds(10), 1, 2, 3, 4);
        var observed = new List<int>();

        await Assert.ThrowsAsync<OperationCanceledException>
        (
            async () =>
            {
                await source.ForEachAsync
                (
                    x =>
                    {
                        observed.Add(x);
                        if (x == 1)
                        {
                            tokenSource.Cancel();
                        }

                        return Task.CompletedTask;
                    },
                    tokenSource.Token
                );
            }
        );
    }



    [Fact]
    public async Task ForEachAsync_Func_when_action_throws_propagates_exception()
    {
        var source = CreateSource(1, 2, 3);

        await Assert.ThrowsAsync<InvalidOperationException>
        (
            () => source.ForEachAsync(x =>
            {
                if (x == 2)
                {
                    throw new InvalidOperationException("test");
                }

                return Task.CompletedTask;
            })
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



    private static async IAsyncEnumerable<int> CreateDelayedSource(TimeSpan delay, params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Delay(delay);
            yield return value;
        }
    }

}
