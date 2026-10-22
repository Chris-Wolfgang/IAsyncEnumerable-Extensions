using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Wolfgang.Extensions.IAsyncEnumerable.Benchmarks;

/// <summary>
/// DistinctUntilChangedAsync / DistinctUntilChangedByAsync over a source made of runs of
/// four equal values: the element form suppresses three of every four elements, and the
/// key form (key = value / 8) suppresses 31 of every 32.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class DistinctUntilChangedAsyncBenchmarks
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
            buffer[i] = i / 4;
        }

        _data = buffer;
    }

    [Benchmark(Baseline = true)]
    public async Task<int> DistinctUntilChangedAsync()
    {
        var count = 0;
        await foreach (var item in CreateSource().DistinctUntilChangedAsync(token: CancellationToken.None))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public async Task<int> DistinctUntilChangedByAsync()
    {
        var count = 0;
        await foreach (var item in CreateSource().DistinctUntilChangedByAsync(static x => x / 8, token: CancellationToken.None))
        {
            count++;
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
