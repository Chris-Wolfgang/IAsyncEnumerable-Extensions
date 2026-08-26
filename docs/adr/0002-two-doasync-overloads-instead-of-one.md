# ADR-0002: Two `DoAsync` overloads instead of a single merged one

## Status

Accepted

## Context

`DoAsync` executes a side-effect on every element as it streams through,
without transforming the sequence — the async analogue of Rx's `Do`. Callers
need it for both a synchronous side effect (`Action<T>`, e.g.
`Console.WriteLine`) and an asynchronous one (`Func<T, Task>`, e.g. an async
logger call). A single merged overload taking `Func<T, Task>` and requiring
synchronous callers to write `x => { DoTheThing(x); return Task.CompletedTask; }`
was considered, as was accepting `Delegate` and dispatching on the runtime
type.

The `Delegate`-based single-overload approach loses compile-time type
checking (a caller could pass an unrelated delegate shape and only find out
at runtime) and pays a reflection-based dispatch cost on every element. The
`Func<T, Task>`-only approach pushes syntactic noise onto every synchronous
call site — the common case, based on the two `<example>` blocks already in
the XML docs (`Console.WriteLine` vs. an async logger).

## Decision

Provide two overloads — `DoAsync(this IAsyncEnumerable<T>, Action<T>, ...)`
and `DoAsync(this IAsyncEnumerable<T>, Func<T, Task>, ...)` — and let normal
C# overload resolution pick the right one from the lambda's inferred shape.
Both funnel into the same `DoCoreAsync` iterator pattern (one core method per
delegate shape) so the streaming/cancellation/disposal logic isn't
duplicated, only the per-element invocation differs.

## Consequences

- Both the synchronous and asynchronous call sites read naturally — no
  `Task.CompletedTask` boilerplate, no runtime type dispatch.
- The public surface has two `DoAsync` signatures to document and keep in
  sync (see the two `<example>` blocks in the XML docs) instead of one — a
  deliberate, bounded cost given the two are genuinely different execution
  shapes.
- Any future third variant (e.g. a `CancellationToken`-aware action,
  `Func<T, CancellationToken, Task>`) follows the same pattern rather than
  collapsing back into a single delegate-typed overload.
