namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

/// <summary>
/// IAsyncEnumerable fake that records whether enumeration has started, used to
/// verify deferred-execution contracts (the source must not be touched until
/// the composed sequence is actually consumed).
/// </summary>
internal sealed class TrackingAsyncEnumerable(params int[] values) : IAsyncEnumerable<int>
{
    public bool EnumerationStarted { get; private set; }



    public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        EnumerationStarted = true;
        return TestSources.Create(values).GetAsyncEnumerator(cancellationToken);
    }
}
