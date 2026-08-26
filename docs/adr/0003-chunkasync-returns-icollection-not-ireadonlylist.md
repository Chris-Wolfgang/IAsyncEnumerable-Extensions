# ADR-0003: `ChunkAsync` returns `ICollection<T>`, not `IReadOnlyList<T>`

## Status

Accepted

## Context

`ChunkAsync` splits a stream into fixed-size batches, yielding each chunk as
it fills. Internally, each chunk is built as a right-sized `T[]` (the
implementation fills a working array up to `maxChunkSize`, then copies the
final partial chunk down to its exact length rather than paying for
`Array.Resize`'s extra bookkeeping). Arrays satisfy both `ICollection<T>`
and `IReadOnlyList<T>`, so the declared return type is a documentation
choice about what a consumer is entitled to rely on, not an implementation
constraint.

`IReadOnlyList<T>` would advertise positional indexing (`chunk[2]`) and
`Count` as part of the contract. `ICollection<T>` advertises `Count` and
enumeration, but not stable positional access. Most consumers of a chunk
only need "how many, and iterate them" — batch-insert, bulk-send,
`foreach`-and-process. Committing to `IReadOnlyList<T>` here would signal
that index-based access is a supported, load-bearing pattern, and would
narrow the door for a future internal representation (e.g. a pooled buffer
or a struct-based span-backed chunk) that supports enumeration and `Count`
cheaply but not O(1) indexing.

## Decision

`ChunkAsync` returns `IAsyncEnumerable<ICollection<T>>`. The current
implementation happens to hand back arrays (which also satisfy
`IReadOnlyList<T>`), but that is an implementation detail, not part of the
public contract.

## Consequences

- Consumers get `Count` without an extra allocation or `ICollection<T>`'s
  own array cast, and can enumerate — enough for the batch-processing use
  case this method exists for.
- A consumer that indexes into a chunk (`chunk[2]`) today will keep working
  (arrays support it), but is relying on implementation detail beyond the
  documented contract; a future change to the internal representation could
  legitimately break that usage without it counting as a breaking API
  change under this library's SemVer contract.
- `ICollection<T>` technically also exposes `Add`/`Remove`/`Clear`. Because
  the underlying object is an array, calling those throws
  `NotSupportedException` at runtime rather than being prevented at compile
  time — a known, accepted gap of `ICollection<T>` versus a true read-only
  collection type, traded for netstandard2.0/net462 compatibility (no
  dependency on `System.Collections.Immutable` or a custom read-only
  wrapper type).
