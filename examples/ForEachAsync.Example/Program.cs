using Wolfgang.Extensions.IAsyncEnumerable;

// ForEachAsync is a terminal operation — it consumes the async stream.
// Has two overloads: Action<T> (sync) and Func<T, Task> (async).

Console.WriteLine("=== ForEachAsync Example ===");
Console.WriteLine();

// Synchronous action: process each item
Console.WriteLine("Processing items with sync action:");
await GenerateNumbers(5).ForEachAsync(x => Console.WriteLine($"  Item: {x}"));
Console.WriteLine();

// Asynchronous action: simulate async processing
Console.WriteLine("Processing items with async action:");
await GenerateNumbers(3).ForEachAsync(async x =>
{
    await Task.Delay(100); // Simulate async work
    Console.WriteLine($"  Processed: {x}");
});
Console.WriteLine();

// Practical example: write each record to a file
Console.WriteLine("Writing records to console (simulating file writes):");
var records = GenerateRecords(4);

await records.ForEachAsync(async record =>
{
    await Task.Delay(50); // Simulate async I/O
    Console.WriteLine($"  Wrote: {record}");
});
Console.WriteLine();

// With cancellation
Console.WriteLine("ForEachAsync with cancellation:");
using var cts = new CancellationTokenSource();

try
{
    await GenerateNumbers(100).ForEachAsync
    (
        x =>
        {
            Console.WriteLine($"  Item: {x}");
            if (x == 3)
            {
                cts.Cancel();
            }
        },
        cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("  Cancelled after item 3.");
}

static async IAsyncEnumerable<int> GenerateNumbers(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return i;
    }
}

static async IAsyncEnumerable<string> GenerateRecords(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return $"Record-{i:D3}";
    }
}
