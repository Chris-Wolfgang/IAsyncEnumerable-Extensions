using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Wolfgang.Extensions.IAsyncEnumerable.Benchmarks;

/// <summary>
/// NoneAsync has two overloads (no predicate / with predicate). The no-predicate
/// variant short-circuits at the first element; the predicate variant walks until
/// it finds a match (worst case: full enumeration when no element matches).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class NoneAsyncBenchmarks
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
    public async Task<bool> NoneAsync_NoPredicate_NonEmpty()
        => await CreateSource().NoneAsync(CancellationToken.None);

    [Benchmark]
    public async Task<bool> NoneAsync_Predicate_NoneMatch()
        => await CreateSource().NoneAsync(static x => x < 0, CancellationToken.None);

    [Benchmark]
    public async Task<bool> NoneAsync_Predicate_FirstMatches()
        => await CreateSource().NoneAsync(static x => x == 0, CancellationToken.None);

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
