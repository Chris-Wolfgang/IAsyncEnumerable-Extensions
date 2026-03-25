using Wolfgang.Extensions.IAsyncEnumerable;

// IsEmptyAsync checks whether an async sequence contains no elements.

Console.WriteLine("=== IsEmptyAsync Example ===");
Console.WriteLine();

// Check an empty async stream
var isEmpty = await GenerateNumbers(0).IsEmptyAsync();
Console.WriteLine($"Empty stream is empty: {isEmpty}");

// Check a populated async stream
var isPopulatedEmpty = await GenerateNumbers(5).IsEmptyAsync();
Console.WriteLine($"Stream with 5 items is empty: {isPopulatedEmpty}");
Console.WriteLine();

// Practical example: check if a query returned results
Console.WriteLine("Checking if search returned results:");

var results = SearchAsync("Widget");

if (await results.IsEmptyAsync())
{
    Console.WriteLine("  No results found for 'Widget'.");
}
else
{
    Console.WriteLine("  Results found!");
}

var noResults = SearchAsync("Nonexistent");

if (await noResults.IsEmptyAsync())
{
    Console.WriteLine("  No results found for 'Nonexistent'.");
}

static async IAsyncEnumerable<int> GenerateNumbers(int count)
{
    for (var i = 1; i <= count; i++)
    {
        await Task.Yield();
        yield return i;
    }
}

static async IAsyncEnumerable<string> SearchAsync(string query)
{
    var products = new[] { "Widget", "Gadget", "Doohickey" };

    foreach (var product in products)
    {
        await Task.Yield();

        if (product.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            yield return product;
        }
    }
}
