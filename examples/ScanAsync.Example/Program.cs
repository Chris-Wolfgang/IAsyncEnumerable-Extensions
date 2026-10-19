using Wolfgang.Extensions.IAsyncEnumerable;

// ScanAsync is Aggregate that emits as it goes: every intermediate accumulator value
// is yielded, one per source element. The seed itself is not yielded.

Console.WriteLine("=== ScanAsync Example ===");
Console.WriteLine();

// Running total
Console.Write("Running total of 1..5: ");
await foreach (var total in GenerateNumbers(5).ScanAsync(0, (sum, x) => sum + x))
{
    Console.Write($"{total} ");
}

Console.WriteLine();
Console.WriteLine();

// Running maximum, with a different accumulator type than the source
Console.WriteLine("Running high-water mark of daily readings:");
await foreach (var (day, high) in GenerateReadings().ScanAsync((Day: 0, High: double.MinValue), (state, reading) =>
    (state.Day + 1, Math.Max(state.High, reading))))
{
    Console.WriteLine($"  Day {day}: highest so far {high:0.0}");
}

Console.WriteLine();

// An empty source yields nothing (no seed value)
var count = 0;
await foreach (var _ in GenerateNumbers(0).ScanAsync(0, (sum, x) => sum + x))
{
    count++;
}

Console.WriteLine($"Values produced from an empty source: {count}");

static async IAsyncEnumerable<int> GenerateNumbers(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return i;
    }
}

static async IAsyncEnumerable<double> GenerateReadings()
{
    foreach (var reading in new[] { 12.5, 14.0, 13.2, 17.8, 16.1 })
    {
        await Task.Yield();
        yield return reading;
    }
}
