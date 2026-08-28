namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

/// <summary>
/// Shared async-sequence generators used across the per-method test files.
/// </summary>
internal static class TestSources
{
    /// <summary>
    /// Yields <paramref name="values"/> in order, with an await between items.
    /// </summary>
    public static async IAsyncEnumerable<T> Create<T>(params T[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
        }
    }



    /// <summary>
    /// Yields <paramref name="values"/> in order, recording each item in
    /// <paramref name="tracker"/> immediately before it is yielded.
    /// </summary>
    public static async IAsyncEnumerable<int> CreateTracking(List<int> tracker, params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            tracker.Add(value);
            yield return value;
        }
    }



    /// <summary>
    /// Yields <paramref name="values"/> in order, canceling
    /// <paramref name="tokenSource"/> after each item is yielded.
    /// </summary>
    public static async IAsyncEnumerable<int> CreateCanceling(CancellationTokenSource tokenSource, params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
            tokenSource.Cancel();
        }
    }
}
