using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Wolfgang.Extensions.IAsyncEnumerable;

namespace ShadowWorkloads;

/// <summary>
/// Realistic scenario 1: a paginated SQL query result streamed as
/// <see cref="IAsyncEnumerable{T}"/> (each "page" yields with a small await
/// to model a round trip), chunked via <c>ChunkAsync</c> into bulk-insert
/// sized batches, and fully consumed — the shape of a change-data-capture
/// or ETL-style migration job. Also doubles as usage documentation for
/// <c>ChunkAsync</c>.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class SqlPaginatedBulkInsertScenario
{
    private const int RowCount = 5_000;
    private const int BulkInsertBatchSize = 500;



    [Benchmark]
    public async Task<int> Scenario_PagedRowsChunkedForBulkInsert()
    {
        var batchCount = 0;

        await foreach (var batch in PagedRows(RowCount).ChunkAsync(BulkInsertBatchSize).ConfigureAwait(false))
        {
            // Stand-in for `await bulkInsert.InsertBatchAsync(batch)`.
            batchCount += batch.Count > 0 ? 1 : 0;
        }

        return batchCount;
    }



    private static async IAsyncEnumerable<Row> PagedRows(int rowCount)
    {
        const int pageSize = 250;

        for (var offset = 0; offset < rowCount; offset += pageSize)
        {
            await Task.Yield();  // models a round trip per page

            var pageLength = Math.Min(pageSize, rowCount - offset);
            for (var i = 0; i < pageLength; i++)
            {
                var id = offset + i;
                yield return new Row(id, $"row-{id}", id * 1.5m);
            }
        }
    }



    private readonly record struct Row(int Id, string Name, decimal Amount);
}
