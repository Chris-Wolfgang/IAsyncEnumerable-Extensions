using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Wolfgang.Extensions.IAsyncEnumerable;

namespace ShadowWorkloads;

/// <summary>
/// Realistic scenario 3: a file streamed line-by-line as
/// <see cref="IAsyncEnumerable{T}"/>, consumed via <c>ForEachAsync</c> with
/// a <see cref="CancellationToken"/> that fires partway through — the
/// shape of a user-cancellable import/tail operation. Also doubles as
/// usage documentation for cooperative cancellation with <c>ForEachAsync</c>.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class FileLineStreamCancellationScenario
{
    private const int LineCount = 5_000;
    private const int CancelAfterLine = LineCount / 2;



    [Benchmark]
    public async Task<int> Scenario_FileLinesCancelledPartway()
    {
        using var cts = new CancellationTokenSource();
        var linesProcessed = 0;

        try
        {
            await FileLines(LineCount).ForEachAsync(_ =>
            {
                linesProcessed++;
                if (linesProcessed == CancelAfterLine)
                {
                    cts.Cancel();
                }
            }, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected: cooperative cancellation mid-stream, not a failure.
        }

        return linesProcessed;
    }



    private static async IAsyncEnumerable<string> FileLines(int lineCount)
    {
        for (var i = 0; i < lineCount; i++)
        {
            if (i % 200 == 0)
            {
                await Task.Yield();  // models a buffered read chunk boundary
            }

            yield return $"line {i}: some log content here";
        }
    }
}
