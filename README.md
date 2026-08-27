# Wolfgang.Extensions.IAsyncEnumerable

A collection of extension methods for `IAsyncEnumerable<T>` in .NET — chunking, side-effect projection, eager iteration, and emptiness / quantifier predicates. Built async-first with strict analyzer enforcement and multi-TFM packaging.

[![NuGet](https://img.shields.io/nuget/v/Wolfgang.Extensions.IAsyncEnumerable.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable)
[![NuGet downloads](https://img.shields.io/nuget/dt/Wolfgang.Extensions.IAsyncEnumerable.svg?logo=nuget&label=downloads)](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable)
[![PR build](https://img.shields.io/github/actions/workflow/status/Chris-Wolfgang/IAsyncEnumerable-Extensions/pr.yaml?event=pull_request_target&label=PR%20build&logo=github)](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/actions/workflows/pr.yaml)
[![Release](https://img.shields.io/github/actions/workflow/status/Chris-Wolfgang/IAsyncEnumerable-Extensions/release.yaml?label=release&logo=github)](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/actions/workflows/release.yaml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-Multi--Targeted-purple.svg)](https://dotnet.microsoft.com/)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-181717?logo=github)](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions)

---

## 📦 Installation

```bash
dotnet add package Wolfgang.Extensions.IAsyncEnumerable
```

**NuGet Package:** [Wolfgang.Extensions.IAsyncEnumerable](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable)

On net462 / netstandard2.0, where `System.Linq.AsyncEnumerable` isn't available, add the companion polyfill package for terminal operators (`CountAsync`, `AnyAsync`, `FirstAsync`, `FirstOrDefaultAsync`, `ToListAsync`):

```bash
dotnet add package Wolfgang.Extensions.IAsyncEnumerable.Polyfill
```

[![NuGet](https://img.shields.io/nuget/v/Wolfgang.Extensions.IAsyncEnumerable.Polyfill.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable.Polyfill)
[![NuGet downloads](https://img.shields.io/nuget/dt/Wolfgang.Extensions.IAsyncEnumerable.Polyfill.svg?logo=nuget&label=downloads)](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable.Polyfill)

**NuGet Package:** [Wolfgang.Extensions.IAsyncEnumerable.Polyfill](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable.Polyfill)

---

## 📄 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

---

## 📚 Documentation

- **GitHub Repository:** [https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions](https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions)
- **API Documentation:** https://Chris-Wolfgang.github.io/IAsyncEnumerable-Extensions/
- **CHANGELOG:** [CHANGELOG.md](CHANGELOG.md)
- **Contributing Guide:** [CONTRIBUTING.md](CONTRIBUTING.md)
- **Formatting Guide:** [docs/README-FORMATTING.md](docs/README-FORMATTING.md)
- **DocFX Version Picker Troubleshooting:** [docs/DOCFX-VERSION-PICKER.md](docs/DOCFX-VERSION-PICKER.md)
- **Release Workflow Setup:** [docs/RELEASE-WORKFLOW-SETUP.md](docs/RELEASE-WORKFLOW-SETUP.md)
- **Workflow Security:** [docs/WORKFLOW_SECURITY.md](docs/WORKFLOW_SECURITY.md)
- **Verify the build:** [docs/REPRODUCIBLE-BUILD.md](docs/REPRODUCIBLE-BUILD.md)

---

## ✨ Extension methods

All nine extensions live on `IAsyncEnumerableExtensions` (`namespace Wolfgang.Extensions.IAsyncEnumerable`). See the [API documentation](https://Chris-Wolfgang.github.io/IAsyncEnumerable-Extensions/) for signatures, parameter details, and per-method examples.

| Method | Purpose |
|---|---|
| `ChunkAsync<T>(source, maxChunkSize, ct)` | Splits the stream into fixed-size `ICollection<T>` batches. |
| `DoAsync<T>(source, Action<T>, ct)` | Side-effect projection — runs a sync action per element; yields the originals unchanged. |
| `DoAsync<T>(source, Func<T, Task>, ct)` | Side-effect projection — runs an async action per element; yields the originals unchanged. |
| `ForEachAsync<T>(source, Action<T>, ct)` | Eager iteration with a sync action — terminal. |
| `ForEachAsync<T>(source, Func<T, Task>, ct)` | Eager iteration with an async action — terminal. |
| `IsEmptyAsync<T>(source, ct)` | `true` if the stream yields zero elements. Short-circuits on the first element. |
| `IsNullOrEmptyAsync<T>(source?, ct)` | `true` if the stream is `null` or yields zero elements. Null-tolerant. |
| `NoneAsync<T>(source, ct)` | `true` if the stream yields zero elements (same shape as `IsEmptyAsync`, different naming). |
| `NoneAsync<T>(source, predicate, ct)` | `true` if no element satisfies the predicate. Short-circuits on the first match. |

### Terminal operators (`Wolfgang.Extensions.IAsyncEnumerable.Polyfill`, net462 / netstandard2.0 only)

These live on `IAsyncEnumerablePolyfillExtensions` (same `Wolfgang.Extensions.IAsyncEnumerable` namespace, separate assembly). They exist only for TFMs where `System.Linq.AsyncEnumerable` isn't available — on net8.0+, use the BCL versions instead.

| Method | Purpose |
|---|---|
| `CountAsync<T>(source, ct)` | Counts the elements in the stream. |
| `AnyAsync<T>(source, ct)` | `true` if the stream yields any elements. Short-circuits on the first element. |
| `AnyAsync<T>(source, predicate, ct)` | `true` if any element satisfies the predicate. Short-circuits on the first match. |
| `FirstAsync<T>(source, ct)` | The first element; throws `InvalidOperationException` if the stream is empty. |
| `FirstOrDefaultAsync<T>(source, ct)` | The first element, or `default(T)` if the stream is empty. |
| `ToListAsync<T>(source, ct)` | Materializes the stream into a `List<T>`. |

---

## 🚀 Quick Start

```csharp
using Wolfgang.Extensions.IAsyncEnumerable;

// Chunk an async stream into batches of up to 100, with logging side-effect
await foreach (var batch in source
    .DoAsync(x => logger.LogInformation("Processing {Item}", x))
    .ChunkAsync(maxChunkSize: 100, token: cancellationToken))
{
    await ProcessBatchAsync(batch);
}
```

---

## 🎯 Supported Frameworks

**Wolfgang.Extensions.IAsyncEnumerable** targets:

- **.NET Framework:** 4.6.2
- **.NET Standard:** 2.0
- **.NET:** 8.0, 10.0

**Wolfgang.Extensions.IAsyncEnumerable.Polyfill** targets:

- **.NET Framework:** 4.6.2
- **.NET Standard:** 2.0

See each package's NuGet page ([main](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable/), [Polyfill](https://www.nuget.org/packages/Wolfgang.Extensions.IAsyncEnumerable.Polyfill/)) for the authoritative per-TFM compatibility matrix.

## 🔍 Code Quality

This project enforces strict analyzer rules and async-first patterns via:

- **Microsoft.CodeAnalysis.NetAnalyzers** (built into the SDK) — correctness + performance
- **Roslynator.Analyzers** — refactoring + style
- **AsyncFixer** — async/await anti-patterns
- **Microsoft.VisualStudio.Threading.Analyzers** — thread-safety + async patterns
- **Microsoft.CodeAnalysis.BannedApiAnalyzers** — banned synchronous APIs (see [`BannedSymbols.txt`](BannedSymbols.txt))
- **Meziantou.Analyzer** — broad code-quality rules
- **SonarAnalyzer.CSharp** — industry-standard analysis
- **Microsoft.CodeAnalysis.PublicApiAnalyzers** — public-API surface change detection (see `src/.../PublicAPI.Shipped.txt`)

`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is active for Release builds, so any analyzer warning blocks the release pipeline.

`BannedSymbols.txt` prohibits the usual sync-over-async traps: `Task.Wait()` / `Task.Result`, `Thread.Sleep`, sync file/stream I/O, `Parallel.For`/`ForEach`, `WebClient`, `BinaryFormatter`.

---

## 🛠️ Building from Source

```bash
git clone https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions.git
cd IAsyncEnumerable-Extensions
dotnet restore
dotnet build --configuration Release
dotnet test  --configuration Release
```

SDK requirement: the version installed by `.github/workflows/pr.yaml` (currently .NET 10 + side-by-side SDKs for the older TFMs). See [`docs/README-FORMATTING.md`](docs/README-FORMATTING.md) for the source-formatting workflow.

---

## 🙏 Acknowledgments

- [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) — `IAsyncEnumerable<T>` backport for `net462` / `netstandard2.0`
- The analyzer-package authors above
- The .NET team for `IAsyncEnumerable<T>` and the async-stream language support that made this library a one-file project
