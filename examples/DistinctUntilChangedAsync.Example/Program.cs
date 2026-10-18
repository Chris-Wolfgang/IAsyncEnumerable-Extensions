using Wolfgang.Extensions.IAsyncEnumerable;

// DistinctUntilChangedAsync drops an element when it equals the one right before it.
// Only ADJACENT repeats are removed, so a value can appear again after a change.

Console.WriteLine("=== DistinctUntilChangedAsync Example ===");
Console.WriteLine();

// Collapse runs of repeated values
Console.Write("1, 1, 2, 2, 2, 3, 1, 1 becomes: ");
await foreach (var item in Generate(1, 1, 2, 2, 2, 3, 1, 1).DistinctUntilChangedAsync())
{
    Console.Write($"{item} ");
}

Console.WriteLine();
Console.WriteLine();

// Report only state transitions, compared case-insensitively
Console.WriteLine("Door sensor transitions:");
await foreach (var state in Generate("open", "OPEN", "Open", "closed", "closed", "open")
    .DistinctUntilChangedAsync(StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"  -> {state}");
}

Console.WriteLine();

// Compare by key (DistinctUntilChangedByAsync): report a reading only when its band changes
Console.WriteLine("Temperature band changes (band = reading / 10):");
await foreach (var reading in Generate(21, 24, 29, 31, 33, 28, 27)
    .DistinctUntilChangedByAsync(reading => reading / 10))
{
    Console.WriteLine($"  {reading} (band {reading / 10})");
}

static async IAsyncEnumerable<T> Generate<T>(params T[] values)
{
    foreach (var value in values)
    {
        await Task.Yield();
        yield return value;
    }
}
