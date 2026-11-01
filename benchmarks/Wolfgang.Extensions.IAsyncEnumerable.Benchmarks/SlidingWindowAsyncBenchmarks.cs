using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Wolfgang.Extensions.IAsyncEnumerable.Benchmarks;

/// <summary>
/// SlidingWindowAsync allocates one circular buffer per enumeration plus one
/// T[WindowSize] per yielded window, so allocation scales with ItemCount * WindowSize.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class SlidingWindowAsyncBenchmarks
{
    private IReadOnlyList<int> _data = [];

    [Params(1024, 4096, 16384)]
    public int ItemCount { get; set; }

    [Params(4, 16, 64)]
    public int WindowSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var buffer = new int[ItemCount];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i;
        }

        _data = buffer;
    }

    [Benchmark(Baseline = true)]
    public async Task<int> SlidingWindowAsync()
    {
        var count = 0;
        await foreach (var window in CreateSource().SlidingWindowAsync(WindowSize, CancellationToken.None))
        {
            count += window.Count;
        }

        return count;
    }

    private async IAsyncEnumerable<int> CreateSource
    (
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        foreach (var value in _data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return value;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
