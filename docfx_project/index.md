---
_layout: landing
---

# Wolfgang.Extensions.IAsyncEnumerable

High-performance, production-grade extension methods for `IAsyncEnumerable<T>` with comprehensive test coverage and strict code quality enforcement.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-Multi--Targeted-purple.svg)](https://dotnet.microsoft.com/)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-181717?logo=github)](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions)

---

## 📦 Installation

```bash
dotnet add package Wolfgang.Extensions.IAsyncEnumerable
```

**NuGet Package:** Available on [NuGet.org](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable/)

---

## 🚀 Quick Start

```csharp
using Wolfgang.Extensions.IAsyncEnumerable;

// Chunk an async stream into batches
await foreach (var chunk in asyncStream.ChunkAsync(maxChunkSize: 100, token: cancellationToken))
{
    // Process each chunk (ICollection<T>)
    await ProcessBatchAsync(chunk);
}
```

---

## ✨ Features

### Current Extension Methods

All 13 methods live on `IAsyncEnumerableExtensions` in the `Wolfgang.Extensions.IAsyncEnumerable` namespace.

#### **`ChunkAsync<T>`**
Splits an `IAsyncEnumerable<T>` into fixed-size chunks for batch processing. The last chunk may be smaller than `maxChunkSize`.

```csharp
public static IAsyncEnumerable<ICollection<T>> ChunkAsync<T>(
    this IAsyncEnumerable<T> source,
    int maxChunkSize,
    CancellationToken token = default)
```

**Example:**
```csharp
var numbers = GetAsyncNumbers(); // IAsyncEnumerable<int>
await foreach (var batch in numbers.ChunkAsync(50))
{
    Console.WriteLine($"Processing batch of {batch.Count} items");
    // Last batch may be smaller than 50
}
```

#### **`DistinctUntilChangedAsync<T>`**
Drops an element that equals the one immediately before it. Only adjacent repeats are removed: `1, 1, 2, 1` becomes `1, 2, 1`. Pass a comparer, or `null` for `EqualityComparer<T>.Default`.

```csharp
public static IAsyncEnumerable<T> DistinctUntilChangedAsync<T>(
    this IAsyncEnumerable<T> source,
    IEqualityComparer<T>? comparer = null,
    CancellationToken token = default)
```

#### **`DistinctUntilChangedByAsync<T, TKey>`**
Same, comparing a key of each element (like `DistinctBy`); yields the first element of each run.

```csharp
public static IAsyncEnumerable<T> DistinctUntilChangedByAsync<T, TKey>(
    this IAsyncEnumerable<T> source,
    Func<T, TKey> keySelector,
    IEqualityComparer<TKey>? comparer = null,
    CancellationToken token = default)
```

**Example:**
```csharp
// Report only when the sign of the reading changes
await foreach (var reading in readings.DistinctUntilChangedByAsync(x => Math.Sign(x)))
{
    Console.WriteLine($"Sign changed at {reading}");
}
```

#### **`DoAsync<T>` (Synchronous action)**
Executes a synchronous side-effect action on each element without transforming the elements. The original items are yielded unchanged — ideal for logging or metrics inside a pipeline.

```csharp
public static IAsyncEnumerable<T> DoAsync<T>(
    this IAsyncEnumerable<T> source,
    Action<T> action,
    CancellationToken token = default)
```

#### **`DoAsync<T>` (Asynchronous action)**
Executes an asynchronous side-effect action on each element without transforming the elements.

```csharp
public static IAsyncEnumerable<T> DoAsync<T>(
    this IAsyncEnumerable<T> source,
    Func<T, Task> action,
    CancellationToken token = default)
```

**Example:**
```csharp
// Chain DoAsync with other extensions for logging/metrics in a pipeline
await foreach (var batch in source
    .DoAsync(async x => await logger.LogAsync($"Processing: {x}"))
    .ChunkAsync(100))
{
    await ProcessBatchAsync(batch);
}
```

#### **`ForEachAsync<T>` (Synchronous action)**
Terminal operation that consumes the sequence, executing a synchronous action on each element.

```csharp
public static Task ForEachAsync<T>(
    this IAsyncEnumerable<T> source,
    Action<T> action,
    CancellationToken token = default)
```

#### **`ForEachAsync<T>` (Asynchronous action)**
Terminal operation that consumes the sequence, awaiting an asynchronous action for each element.

```csharp
public static Task ForEachAsync<T>(
    this IAsyncEnumerable<T> source,
    Func<T, Task> action,
    CancellationToken token = default)
```

**Example:**
```csharp
await source.ForEachAsync(x => Console.WriteLine($"Item: {x}"));
await source.ForEachAsync(async x => await logger.LogAsync($"Item: {x}"));
```

#### **`IsEmptyAsync<T>`**
Determines whether the sequence contains no elements. Stops after checking the first element.

```csharp
public static Task<bool> IsEmptyAsync<T>(
    this IAsyncEnumerable<T> source,
    CancellationToken token = default)
```

#### **`IsNullOrEmptyAsync<T>`**
Determines whether the sequence is `null` or contains no elements. The only method that accepts a `null` receiver.

```csharp
public static Task<bool> IsNullOrEmptyAsync<T>(
    this IAsyncEnumerable<T>? source,
    CancellationToken token = default)
```

#### **`NoneAsync<T>` (No predicate)**
Determines whether the sequence contains no elements — the inverse of `Any`.

```csharp
public static Task<bool> NoneAsync<T>(
    this IAsyncEnumerable<T> source,
    CancellationToken token = default)
```

#### **`NoneAsync<T>` (Predicate)**
Determines whether no element satisfies a condition. Short-circuits on the first match.

```csharp
public static Task<bool> NoneAsync<T>(
    this IAsyncEnumerable<T> source,
    Func<T, bool> predicate,
    CancellationToken token = default)
```

**Example:**
```csharp
if (await source.NoneAsync(x => x.IsExpired))
{
    Console.WriteLine("No expired items.");
}
```

#### **`ScanAsync<TSource, TAccumulate>`**
Running accumulation: yields every intermediate value, like `Aggregate` emitting at each step. The seed itself is not yielded, so an empty source yields nothing.

```csharp
public static IAsyncEnumerable<TAccumulate> ScanAsync<TSource, TAccumulate>(
    this IAsyncEnumerable<TSource> source,
    TAccumulate seed,
    Func<TAccumulate, TSource, TAccumulate> accumulator,
    CancellationToken token = default)
```

**Example:**
```csharp
// Running sum: 1, 2, 3, 4 produces 1, 3, 6, 10
await foreach (var total in numbers.ScanAsync(0, (sum, x) => sum + x))
{
    Console.WriteLine(total);
}
```

#### **`SlidingWindowAsync<T>`**
Overlapping windows of `windowSize` consecutive elements as `IReadOnlyList<T>`, advancing one element at a time. A source shorter than `windowSize` yields nothing; every window is a new list the consumer may keep.

```csharp
public static IAsyncEnumerable<IReadOnlyList<T>> SlidingWindowAsync<T>(
    this IAsyncEnumerable<T> source,
    int windowSize,
    CancellationToken token = default)
```

**Example:**
```csharp
// Moving average over the last three readings
await foreach (var window in readings.SlidingWindowAsync(3))
{
    Console.WriteLine((window[0] + window[1] + window[2]) / 3.0);
}
```

### Legacy Terminal Operators (`Wolfgang.Extensions.IAsyncEnumerable.Legacy`)

A companion package for consumers on **net462 / netstandard2.0**, where the BCL's `System.Linq.AsyncEnumerable` is not available. It provides 6 terminal operators on `IAsyncEnumerableLegacyExtensions` (same `Wolfgang.Extensions.IAsyncEnumerable` namespace):

- `CountAsync<T>` — counts the elements in the sequence
- `AnyAsync<T>` — determines whether the sequence contains any elements
- `AnyAsync<T>(predicate)` — determines whether any element satisfies a condition
- `FirstAsync<T>` — returns the first element (throws if empty)
- `FirstOrDefaultAsync<T>` — returns the first element, or `default(T)` if empty
- `ToListAsync<T>` — materializes the sequence into a `List<T>`

The package targets **only** net462 and netstandard2.0, so there is no ambiguous-call risk with `System.Linq.AsyncEnumerable` on net8.0+ — it simply doesn't apply there.

```bash
dotnet add package Wolfgang.Extensions.IAsyncEnumerable.Legacy
```

---

## 🎯 Target Frameworks

This library supports multiple .NET versions:
- **.NET Framework 4.6.2** (`net462`)
- **.NET Standard 2.0** (`netstandard2.0`)
- **.NET 8.0** (`net8.0`)
- **.NET 10.0** (`net10.0`)

---

## 🔍 Code Quality & Static Analysis

This project enforces **strict code quality standards** through **8 specialized analyzers** and custom async-first rules:

### Analyzers in Use

1. **Microsoft.CodeAnalysis.NetAnalyzers** - Built-in .NET analyzers for correctness and performance
2. **Roslynator.Analyzers** - Advanced refactoring and code quality rules
3. **AsyncFixer** - Async/await best practices and anti-pattern detection
4. **Microsoft.VisualStudio.Threading.Analyzers** - Thread safety and async patterns
5. **Microsoft.CodeAnalysis.BannedApiAnalyzers** - Prevents usage of banned synchronous APIs
6. **Meziantou.Analyzer** - Comprehensive code quality rules
7. **SonarAnalyzer.CSharp** - Industry-standard code analysis
8. **Microsoft.CodeAnalysis.PublicApiAnalyzers** - Public API surface change detection

### Async-First Enforcement

This library uses **`BannedSymbols.txt`** to prohibit synchronous APIs and enforce async-first patterns:

**Blocked APIs Include:**
- ❌ `Task.Wait()`, `Task.Result` - Use `await` instead
- ❌ `Thread.Sleep()` - Use `await Task.Delay()` instead
- ❌ Synchronous file I/O (`File.ReadAllText`) - Use async versions
- ❌ Synchronous stream operations - Use `ReadAsync()`, `WriteAsync()`
- ❌ `Parallel.For/ForEach` - Use `Task.WhenAll()` or `Parallel.ForEachAsync()`
- ❌ Obsolete APIs (`WebClient`, `BinaryFormatter`)

**Why?** To ensure all code is **truly async** and **non-blocking** for optimal performance in async contexts.

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- Optional: [PowerShell Core](https://github.com/PowerShell/PowerShell) for formatting scripts

### Build Steps

```bash
# Clone the repository
git clone https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions.git
cd IAsyncEnumerable-Extensions

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Run code formatting (PowerShell Core)
pwsh ./scripts/format.ps1
```

### Code Formatting

This project uses `.editorconfig` and `dotnet format`:

```bash
# Format code
dotnet format

# Verify formatting (as CI does)
dotnet format --verify-no-changes
```

See [README-FORMATTING.md](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/blob/main/docs/README-FORMATTING.md) for detailed formatting guidelines.

### Building Documentation

This project uses [DocFX](https://dotnet.github.io/docfx/) to generate API documentation:

```bash
# Install DocFX (one-time setup)
dotnet tool install -g docfx

# Generate API metadata and build documentation
cd docfx_project
docfx metadata  # Extract API metadata from source code
docfx build     # Build HTML documentation

# Documentation is generated in docfx_project/_site/
```

The documentation is automatically built and deployed to GitHub Pages when changes are pushed to the `main` branch.

**Local Preview:**
```bash
# Serve documentation locally (with live reload)
cd docfx_project
docfx build --serve

# Open http://localhost:8080 in your browser
```

**Documentation Structure:**
- `docfx_project/` - DocFX configuration and source files
- `docfx_project/_site/` - Generated HTML documentation (published to GitHub Pages)
- `docfx_project/index.md` - Main landing page content
- `docfx_project/docs/` - Additional documentation articles
- `docfx_project/api/` - Auto-generated API reference YAML files

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/blob/main/CONTRIBUTING.md) for:
- Code quality standards
- Build and test instructions
- Pull request guidelines
- Analyzer configuration details

---

## 📄 License

This project is licensed under the **MIT License**. See the [LICENSE](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/blob/main/LICENSE) file for details.

---

## 📚 Documentation

- **GitHub Repository:** [https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions)
- **API Documentation:** [Latest](https://chris-wolfgang.github.io/IAsyncEnumerable-Extensions/versions/latest/api/Wolfgang.Extensions.IAsyncEnumerable.html)
- **Formatting Guide:** [README-FORMATTING.md](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/blob/main/docs/README-FORMATTING.md)
- **Contributing Guide:** [CONTRIBUTING.md](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/blob/main/CONTRIBUTING.md)

---

## 🙏 Acknowledgments

Built with:
- [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) for backward compatibility
- Comprehensive analyzer packages for code quality enforcement
- .NET async/await patterns for optimal performance
