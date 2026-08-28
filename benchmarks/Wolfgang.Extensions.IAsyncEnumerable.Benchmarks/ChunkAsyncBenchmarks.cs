using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Wolfgang.Extensions.IAsyncEnumerable.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ChunkAsyncBenchmarks
{
    private IReadOnlyList<int> _data = [];

    [Params(1024, 4096, 16384)]
    public int ItemCount { get; set; }

    [Params(4, 16, 64)]
    public int ChunkSize { get; set; }

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
    public async Task<int> ChunkAsync()
        => await ConsumeAsync(static (source, size, token) => source.ChunkAsync(size, token));



    private async Task<int> ConsumeAsync
    (
        Func<IAsyncEnumerable<int>, int, CancellationToken, IAsyncEnumerable<ICollection<int>>> chunker
    )
    {
        var count = 0;
        await foreach (var chunk in chunker(CreateSource(), ChunkSize, CancellationToken.None))
        {
            count += chunk.Count;
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
