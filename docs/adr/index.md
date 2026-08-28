# Architecture Decision Records

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-pin-assemblyversion-to-1.0.0.0.md) | Pin AssemblyVersion to a fixed 1.0.0.0 baseline | Accepted |
| [0002](0002-two-doasync-overloads-instead-of-one.md) | Two `DoAsync` overloads instead of a single merged one | Accepted |
| [0003](0003-chunkasync-returns-icollection-not-ireadonlylist.md) | `ChunkAsync` returns `ICollection<T>`, not `IReadOnlyList<T>` | Accepted |
| [0004](0004-banned-symbols-enforce-async-first.md) | `BannedSymbols.txt` enforces async-first, not just "no blocking calls" | Accepted |
| [0005](0005-legacy-package-conventions.md) | Legacy package conventions (TFM scope, ValueTask returns, derived AssemblyVersion) | Accepted |

New ADRs land alongside the PR that introduces the corresponding decision —
see [TEMPLATE.md](TEMPLATE.md).
