# ADR-0006: `SlidingWindowAsync` returns `IReadOnlyList<T>`, unlike `ChunkAsync`

## Status

Accepted

## Context

[ADR-0003](0003-chunkasync-returns-icollection-not-ireadonlylist.md) chose
`ICollection<T>` for `ChunkAsync` because a batch is consumed as "how many,
then iterate", and advertising positional indexing would commit the library to
O(1) indexing it might want to give up for a pooled or span-backed chunk later.

`SlidingWindowAsync` (issue #123) yields overlapping windows: `1, 2, 3, 4, 5`
with a window of 3 gives `[1, 2, 3]`, `[2, 3, 4]`, `[3, 4, 5]`. The reasons to
take a window are positional: a moving average over fixed slots, comparing the
oldest element with the newest (`window[0]` and `window[^1]`), or reading a
fixed offset for trend and pattern detection. Without indexing, every consumer
would copy the window into a list first, which defeats the purpose.

The window cannot be a view over the operator's internal buffer either. The
buffer is rewritten as the next element arrives, so a consumer that kept a
window would see it change under them. Each window has to be its own copy
anyway, and a fresh array costs the same whether it is advertised as
`ICollection<T>` or `IReadOnlyList<T>`.

## Decision

`SlidingWindowAsync` returns `IAsyncEnumerable<IReadOnlyList<T>>`. Each window
is a new array in oldest-first order, independent of later windows.

## Consequences

- Indexing into a window is part of the documented contract, so it cannot be
  withdrawn later without a breaking change. That is accepted, because it is
  the operator's main use.
- The repository now has two collection conventions for operators that yield
  groups: batch-shaped results (`ChunkAsync`) use `ICollection<T>`, and
  window-shaped results (`SlidingWindowAsync`) use `IReadOnlyList<T>`. A future
  operator should pick by the same test: is positional access the reason to
  call it?
- `IReadOnlyList<T>` is available on every target framework the package
  supports (net462, netstandard2.0, net8.0, net10.0), so no dependency is added.
- One `windowSize`-length array is allocated per window. A pooled, shared-buffer
  variant would be faster but would break the "a window you keep stays valid"
  guarantee, so it is not offered.
