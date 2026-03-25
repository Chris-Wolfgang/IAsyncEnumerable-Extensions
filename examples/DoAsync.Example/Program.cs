using Wolfgang.Extensions.IAsyncEnumerable;

// DoAsync executes a side-effect on each element without transforming it.
// Items are yielded unchanged — it's a passthrough for pipelines.
// Has two overloads: Action<T> (sync) and Func<T, Task> (async).

Console.WriteLine("=== DoAsync Example ===");
Console.WriteLine();

// Synchronous action: logging items as they flow through
Console.WriteLine("Logging items mid-pipeline (sync action):");
var results = new List<int>();

await foreach (var item in GenerateNumbers(5).DoAsync(x => Console.WriteLine($"  Processing: {x}")))
{
    results.Add(item);
}

Console.WriteLine($"Collected: [{string.Join(", ", results)}]");
Console.WriteLine();

// Asynchronous action: simulate async logging
Console.WriteLine("Logging items mid-pipeline (async action):");

await foreach (var item in GenerateNumbers(3).DoAsync(async x =>
{
    await Task.Delay(50); // Simulate async logging
    Console.WriteLine($"  Logged item: {x}");
}))
{
    Console.WriteLine($"  Yielded: {item}");
}

Console.WriteLine();

// Chaining DoAsync with ChunkAsync for observability
Console.WriteLine("Pipeline: DoAsync -> ChunkAsync:");
var count = 0;

await foreach (var batch in GenerateNumbers(8)
    .DoAsync(_ => count++)
    .ChunkAsync(3))
{
    Console.WriteLine($"  Batch: [{string.Join(", ", batch)}]");
}

Console.WriteLine($"Total items observed by DoAsync: {count}");

static async IAsyncEnumerable<int> GenerateNumbers(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return i;
    }
}
