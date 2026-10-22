using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Wolfgang.Extensions.IAsyncEnumerable.Benchmarks;

/// <summary>
/// ScanAsync yields one running value per element; the accumulator itself is
/// allocation-free here, so the measured allocation is the operator's own state machine.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ScanAsyncBenchmarks
{
    private IReadOnlyList<int> _data = [];

    [Params(1024, 4096, 16384)]
    public int ItemCount { get; set; }

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
    public async Task<long> ScanAsync_RunningSum()
    {
        var last = 0L;
        await foreach (var total in CreateSource().ScanAsync(0L, static (sum, x) => sum + x, CancellationToken.None))
        {
            last = total;
        }

        return last;
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
