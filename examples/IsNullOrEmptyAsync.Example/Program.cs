using Wolfgang.Extensions.IAsyncEnumerable;

// IsNullOrEmptyAsync safely checks whether an async sequence is null or empty.
// Unlike IsEmptyAsync, it does NOT throw on null — it returns true instead.

Console.WriteLine("=== IsNullOrEmptyAsync Example ===");
Console.WriteLine();

// Null reference
IAsyncEnumerable<int>? nullStream = null;
Console.WriteLine($"Null stream: {await nullStream.IsNullOrEmptyAsync()}");

// Empty stream
var emptyStream = GenerateNumbers(0);
Console.WriteLine($"Empty stream: {await emptyStream.IsNullOrEmptyAsync()}");

// Populated stream
var populatedStream = GenerateNumbers(3);
Console.WriteLine($"Populated stream: {await populatedStream.IsNullOrEmptyAsync()}");
Console.WriteLine();

// Practical example: guard clause with nullable async source
Console.WriteLine("Processing with null guard:");
await ProcessDataAsync(null);
await ProcessDataAsync(GenerateNumbers(0));
await ProcessDataAsync(GenerateNumbers(3));

static async Task ProcessDataAsync(IAsyncEnumerable<int>? data)
{
    if (await data.IsNullOrEmptyAsync())
    {
        Console.WriteLine("  No data to process.");
        return;
    }

    Console.Write("  Data: ");
    var items = new List<int>();

    await foreach (var item in data!)
    {
        items.Add(item);
    }

    Console.WriteLine(string.Join(", ", items));
}

static async IAsyncEnumerable<int> GenerateNumbers(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return i;
    }
}
