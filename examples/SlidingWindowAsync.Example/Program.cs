using Wolfgang.Extensions.IAsyncEnumerable;

// SlidingWindowAsync yields overlapping windows of a fixed size, advancing one element
// at a time. The first window appears once enough elements have arrived, and every
// window is its own list, so it is safe to keep.

Console.WriteLine("=== SlidingWindowAsync Example ===");
Console.WriteLine();

// Windows of three over 1..5
Console.WriteLine("Windows of 3 over 1..5:");
await foreach (var window in Generate(1, 2, 3, 4, 5).SlidingWindowAsync(3))
{
    Console.WriteLine($"  [{string.Join(", ", window)}]");
}

Console.WriteLine();

// Three-reading moving average
Console.WriteLine("Three-reading moving average:");
await foreach (var window in Generate(10.0, 12.0, 11.0, 15.0, 18.0, 17.0).SlidingWindowAsync(3))
{
    var average = (window[0] + window[1] + window[2]) / 3;
    Console.WriteLine($"  [{string.Join(", ", window)}] -> {average:0.00}");
}

Console.WriteLine();

// Trend detection: compare the oldest and newest element of each window
Console.WriteLine("Rising over a window of 4?");
await foreach (var window in Generate(5, 6, 4, 7, 9, 3, 2).SlidingWindowAsync(4))
{
    var trend = window[window.Count - 1] > window[0] ? "rising" : "not rising";
    Console.WriteLine($"  [{string.Join(", ", window)}] -> {trend}");
}

static async IAsyncEnumerable<T> Generate<T>(params T[] values)
{
    foreach (var value in values)
    {
        await Task.Yield();
        yield return value;
    }
}
