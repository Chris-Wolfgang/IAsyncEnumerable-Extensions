# Getting Started

This guide will help you install and start using Wolfgang.Extensions.IAsyncEnumerable in your .NET projects.

## Installation

### Using .NET CLI

Add the package to your project using the .NET CLI:

```bash
dotnet add package Wolfgang.Extensions.IAsyncEnumerable
```

### Using Package Manager Console

In Visual Studio, use the Package Manager Console:

```powershell
Install-Package Wolfgang.Extensions.IAsyncEnumerable
```

### Using Visual Studio NuGet Package Manager

1. Right-click your project in Solution Explorer
2. Select "Manage NuGet Packages..."
3. Search for "Wolfgang.Extensions.IAsyncEnumerable"
4. Click "Install"

> **Note:** The NuGet package is coming soon to NuGet.org. Check the [GitHub repository](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions) for the latest status.

## Prerequisites

The library supports multiple .NET versions:

- **.NET Framework 4.6.2** or later
- **.NET Standard 2.0** compatible projects
- **.NET 8.0** or later
- **.NET 10.0** or later

## Basic Usage

### Import the Namespace

Add the using directive to your C# file:

```csharp
using Wolfgang.Extensions.IAsyncEnumerable;
```

### Example: Chunking an Async Stream

The most common use case is processing a large async stream in batches:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Extensions.IAsyncEnumerable;

public class Example
{
    public async Task ProcessDataAsync(CancellationToken cancellationToken = default)
    {
        // Get an async stream from a database, API, or other source
        IAsyncEnumerable<DataRecord> dataStream = GetAsyncDataStream();

        // Process the stream in chunks of 100
        await foreach (var chunk in dataStream.ChunkAsync(maxChunkSize: 100, token: cancellationToken))
        {
            Console.WriteLine($"Processing batch of {chunk.Count} records");
            
            // Process the batch (e.g., bulk insert to database)
            await ProcessBatchAsync(chunk);
        }
    }

    private async IAsyncEnumerable<DataRecord> GetAsyncDataStream()
    {
        // Simulate an async data source
        for (int i = 0; i < 1000; i++)
        {
            await Task.Delay(10); // Simulate async operation
            yield return new DataRecord { Id = i };
        }
    }

    private async Task ProcessBatchAsync(ICollection<DataRecord> batch)
    {
        // Simulate batch processing
        await Task.Delay(100);
        Console.WriteLine($"Processed {batch.Count} records");
    }
}

public record DataRecord
{
    public int Id { get; init; }
}
```

## ChunkAsync Method Details

### Method Signature

```csharp
public static IAsyncEnumerable<ICollection<T>> ChunkAsync<T>(
    this IAsyncEnumerable<T> source,
    int maxChunkSize,
    CancellationToken token = default)
```

### Parameters

- **source**: The source `IAsyncEnumerable<T>` to chunk
- **maxChunkSize**: Maximum number of elements in each chunk (must be > 0)
- **token**: Optional cancellation token to cancel the operation

### Returns

An `IAsyncEnumerable<ICollection<T>>` where each element is a collection containing up to `maxChunkSize` items from the source.

### Important Notes

1. **Last Chunk Size**: The last chunk may contain fewer than `maxChunkSize` elements if the source doesn't divide evenly
2. **Cancellation**: Pass a `CancellationToken` to enable responsive cancellation of long-running operations
3. **Thread Safety**: The method is safe to use with async streams from any source
4. **Memory Efficiency**: Each chunk is yielded as soon as it's full, minimizing memory overhead

## Advanced Examples

### Example 1: Database Batch Insert

```csharp
public async Task BulkInsertAsync(IAsyncEnumerable<User> users, CancellationToken ct)
{
    await foreach (var batch in users.ChunkAsync(maxChunkSize: 500, token: ct))
    {
        // Use batch insert for better performance
        await _dbContext.Users.AddRangeAsync(batch, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        Console.WriteLine($"Inserted {batch.Count} users");
    }
}
```

### Example 2: API Rate Limiting

```csharp
public async Task ProcessApiRequests(IAsyncEnumerable<ApiRequest> requests, CancellationToken ct)
{
    await foreach (var batch in requests.ChunkAsync(maxChunkSize: 10, token: ct))
    {
        // Process batch concurrently, respecting API rate limits
        var tasks = batch.Select(req => _apiClient.SendAsync(req, ct));
        await Task.WhenAll(tasks);
        
        // Wait before next batch to respect rate limits
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
    }
}
```

### Example 3: Parallel Processing with Controlled Concurrency

```csharp
public async Task ProcessWithControlledConcurrency(
    IAsyncEnumerable<WorkItem> items, 
    CancellationToken ct)
{
    await foreach (var batch in items.ChunkAsync(maxChunkSize: 20, token: ct))
    {
        // Process each batch item in parallel, but limit total concurrency
        var tasks = batch.Select(item => ProcessItemAsync(item, ct));
        await Task.WhenAll(tasks);
    }
}
```

## Error Handling

Always use try-catch blocks when working with async enumerables:

```csharp
public async Task SafeProcessingAsync(IAsyncEnumerable<DataItem> items, CancellationToken ct)
{
    try
    {
        await foreach (var chunk in items.ChunkAsync(maxChunkSize: 100, token: ct))
        {
            await ProcessChunkAsync(chunk);
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Processing was cancelled");
        throw; // Re-throw to let caller handle cancellation
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing items: {ex.Message}");
        throw;
    }
}
```

## Performance Tips

1. **Choose Appropriate Chunk Sizes**: Balance between memory usage and processing efficiency
   - Smaller chunks (10-50): Lower memory, more overhead
   - Medium chunks (100-500): Good balance for most scenarios
   - Large chunks (1000+): Better throughput, higher memory usage

2. **Use Cancellation Tokens**: Always pass cancellation tokens for responsive applications

3. **Avoid Blocking Operations**: Never use `Task.Wait()` or `Task.Result` - always use `await`

4. **Profile Your Code**: Use BenchmarkDotNet or similar tools to measure performance in your specific scenario

## ForEachAsync - Terminal Iteration

Consume an async stream with an action:

```csharp
// Synchronous action
await dataStream.ForEachAsync(item => Console.WriteLine($"Processing: {item.Id}"));

// Asynchronous action
await dataStream.ForEachAsync(async item => await SaveToDbAsync(item));
```

## DoAsync - Passthrough Side Effects

Execute a side-effect without consuming the stream:

```csharp
await foreach (var batch in dataStream
    .DoAsync(x => logger.LogDebug($"Item: {x.Id}"))
    .ChunkAsync(100))
{
    await ProcessBatchAsync(batch);
}
```

## IsEmptyAsync / IsNullOrEmptyAsync

```csharp
if (await dataStream.IsEmptyAsync(cancellationToken))
{
    Console.WriteLine("No data available");
}

IAsyncEnumerable<DataRecord>? maybeNull = GetData();
if (await maybeNull.IsNullOrEmptyAsync())
{
    Console.WriteLine("Data is null or empty");
}
```

## NoneAsync - Inverse of Any

```csharp
// Check if stream has no elements
if (await dataStream.NoneAsync())
{
    Console.WriteLine("Empty stream");
}

// Check if no elements match a condition
if (await dataStream.NoneAsync(x => x.IsExpired, cancellationToken))
{
    Console.WriteLine("No expired records found");
}
```

## Next Steps

- Read the [Introduction](introduction.md) to learn more about the library's philosophy
- Check the [GitHub repository](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions) for examples and source code
- Review [CONTRIBUTING.md](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/blob/main/CONTRIBUTING.md) if you'd like to contribute

## Need Help?

- **Issues**: Report bugs or request features on [GitHub Issues](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/issues)
- **Discussions**: Ask questions in [GitHub Discussions](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/discussions)
- **Source Code**: View the source on [GitHub](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions)