using NoneAsyncExample;
using Wolfgang.Extensions.IAsyncEnumerable;

// NoneAsync is the async inverse of Any.
// NoneAsync() returns true if the sequence has no elements.
// NoneAsync(predicate) returns true if no elements match the predicate.
// The predicate overload short-circuits on the first match.

Console.WriteLine("=== NoneAsync Example ===");
Console.WriteLine();

// NoneAsync() — check if stream is empty
var emptyStream = GenerateNumbers(0);
var populatedStream = GenerateNumbers(5);

Console.WriteLine($"Empty stream has none: {await emptyStream.NoneAsync()}");
Console.WriteLine($"Populated stream has none: {await populatedStream.NoneAsync()}");
Console.WriteLine();

// NoneAsync(predicate) — check if no elements match
Console.WriteLine($"None divisible by 3: {await GenerateNumbers(10).NoneAsync(n => n % 3 == 0)}");
Console.WriteLine($"None greater than 100: {await GenerateNumbers(10).NoneAsync(n => n > 100)}");
Console.WriteLine($"None negative: {await GenerateNumbers(10).NoneAsync(n => n < 0)}");
Console.WriteLine();

// Practical example: validate async data stream
Console.WriteLine("Validating order stream:");

var orders = GenerateOrders();

if (await orders.NoneAsync(o => o.Amount <= 0))
{
    Console.WriteLine("  All orders have valid amounts.");
}
else
{
    Console.WriteLine("  Some orders have invalid amounts.");
}

// With cancellation
Console.WriteLine();
Console.WriteLine("NoneAsync with cancellation:");
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var result = await GenerateNumbers(5).NoneAsync(n => n > 10, cts.Token);
Console.WriteLine($"  None greater than 10: {result}");

static async IAsyncEnumerable<int> GenerateNumbers(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return i;
    }
}

static async IAsyncEnumerable<Order> GenerateOrders()
{
    var orders = new[]
    {
        new Order("ORD-001", 29.99m),
        new Order("ORD-002", 49.99m),
        new Order("ORD-003", 15.00m),
    };

    foreach (var order in orders)
    {
        await Task.Yield();
        yield return order;
    }
}

namespace NoneAsyncExample
{
    internal record Order(string Id, decimal Amount);
}
