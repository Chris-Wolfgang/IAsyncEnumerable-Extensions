using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Wolfgang.Extensions.IAsyncEnumerable.Benchmarks;

/// <summary>
/// IsEmptyAsync and IsNullOrEmptyAsync are O(1) on the first element — these benchmarks
/// also exercise the non-empty path (must materialize the first element via MoveNextAsync)
/// to keep the comparison honest.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class EmptyCheckAsyncBenchmarks
{
    private IReadOnlyList<int> _empty = [];
    private IReadOnlyList<int> _nonEmpty = [];

    [GlobalSetup]
    public void Setup()
    {
        _empty = [];
        _nonEmpty = [1, 2, 3];
    }

    [Benchmark(Baseline = true)]
    public async Task<bool> IsEmptyAsync_Empty()
        => await CreateSource(_empty).IsEmptyAsync(CancellationToken.None);

    [Benchmark]
    public async Task<bool> IsEmptyAsync_NonEmpty()
        => await CreateSource(_nonEmpty).IsEmptyAsync(CancellationToken.None);

    [Benchmark]
    public async Task<bool> IsNullOrEmptyAsync_Null()
        => await IAsyncEnumerableExtensions.IsNullOrEmptyAsync<int>(source: null, token: CancellationToken.None);

    [Benchmark]
    public async Task<bool> IsNullOrEmptyAsync_Empty()
        => await CreateSource(_empty).IsNullOrEmptyAsync(CancellationToken.None);

    [Benchmark]
    public async Task<bool> IsNullOrEmptyAsync_NonEmpty()
        => await CreateSource(_nonEmpty).IsNullOrEmptyAsync(CancellationToken.None);

    private async IAsyncEnumerable<int> CreateSource
    (
        IReadOnlyList<int> data,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        foreach (var value in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return value;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
