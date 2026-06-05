using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Wolfgang.Extensions.IAsyncEnumerable.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ForEachAsyncBenchmarks
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
    public async Task<int> ForEachAsync_SyncAction()
    {
        var sum = 0;
        await CreateSource().ForEachAsync(x => sum += x, CancellationToken.None);
        return sum;
    }

    [Benchmark]
    public async Task<int> ForEachAsync_AsyncAction()
    {
        var sum = 0;
        await CreateSource().ForEachAsync(x => { sum += x; return Task.CompletedTask; }, CancellationToken.None);
        return sum;
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
