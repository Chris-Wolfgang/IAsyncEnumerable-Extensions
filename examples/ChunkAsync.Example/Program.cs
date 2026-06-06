using ChunkAsyncExample;
using Wolfgang.Extensions.IAsyncEnumerable;

// ChunkAsync splits an IAsyncEnumerable<T> into fixed-size chunks.
// Useful for batch processing of async streams.

Console.WriteLine("=== ChunkAsync Example ===");
Console.WriteLine();

// Basic chunking: split 10 items into batches of 3
Console.WriteLine("Chunking 10 items into batches of 3:");
var batchNumber = 1;

await foreach (var chunk in GenerateNumbers(10).ChunkAsync(3))
{
    Console.WriteLine($"  Batch {batchNumber++}: [{string.Join(", ", chunk)}] (count: {chunk.Count})");
}

Console.WriteLine();

// Practical example: batch database inserts
Console.WriteLine("Simulated batch insert of 7 records (batch size: 3):");
var totalInserted = 0;

await foreach (var batch in GenerateRecords(7).ChunkAsync(3))
{
    // In real code: await db.BulkInsertAsync(batch);
    totalInserted += batch.Count;
    Console.WriteLine($"  Inserted batch of {batch.Count} records (total: {totalInserted})");
}

Console.WriteLine();

// With cancellation support
Console.WriteLine("Chunking with cancellation token:");
using var cts = new CancellationTokenSource();

await foreach (var chunk in GenerateNumbers(20).ChunkAsync(5, cts.Token))
{
    Console.WriteLine($"  Chunk: [{string.Join(", ", chunk)}]");

    if (chunk.First() >= 10)
    {
        Console.WriteLine("  Stopping after reaching 10.");
        break;
    }
}

static async IAsyncEnumerable<int> GenerateNumbers(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return i;
    }
}

static async IAsyncEnumerable<Record> GenerateRecords(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return new Record(i, $"Record-{i}");
    }
}

namespace ChunkAsyncExample
{
    internal record Record(int Id, string Name);
}
