# Allocation policy

## No public method is documented as zero-allocation

Every method on `IAsyncEnumerableExtensions` either compiles to an `async`
state machine (`DoAsync`, `ForEachAsync`, `IsEmptyAsync`,
`IsNullOrEmptyAsync`, `NoneAsync`) or an async-iterator state machine
(`ChunkAsync`'s `ChunkCoreAsync`, `DoAsync`'s `DoCoreAsync`) — both allocate
a heap-based state-machine object on `netstandard2.0`/`net462` by design
(the .NET runtime does not stack-allocate async/iterator state machines on
those TFMs). `ChunkAsync` additionally allocates a right-sized `T[]` per
chunk — that allocation is the method's entire purpose, not a hot-path
regression to eliminate.

Given that, an opt-in `[NoAlloc]`/zero-byte-assertion test suite (the shape
`#242` originally asked for) would either assert something already false
by design, or exclude every method from the check — a check with nothing
left to check isn't worth the maintenance weight. This is the documented
"skip" outcome `#242` itself allows for: *"Skip on libraries where no
method is intended to be zero-alloc (snapshot doc explaining why instead
of half-implemented enforcement)."* This file is that snapshot.

## The Legacy package (`Wolfgang.Extensions.IAsyncEnumerable.Legacy`)

The same policy applies to `IAsyncEnumerableLegacyExtensions`. All 6 terminal
operators (`CountAsync`, `AnyAsync` × 2, `FirstAsync`, `FirstOrDefaultAsync`,
`ToListAsync`) compile to `async` state machines, and the package targets
only net462/netstandard2.0 — TFMs with none of the cached-task /
`IValueTaskSource` optimizations newer runtimes use — so none of them is
zero-alloc, by design. `ToListAsync` additionally allocates the returned
`List<T>`; that allocation is the method's purpose. Beyond the state machine
and the enumerator the source itself provides, there are no other
per-element allocations, and the eager-validation wrapper methods allocate
nothing on the happy path (validation happens before any state machine is
created).

## What's already in place instead

`[MemoryDiagnoser]` is enabled on every `BenchmarkDotNet` benchmark class
under `benchmarks/` (one per public method — see
`ChunkAsyncBenchmarks.cs`, `DoAsyncBenchmarks.cs`, `ForEachAsyncBenchmarks.cs`,
`EmptyCheckAsyncBenchmarks.cs`, `NoneAsyncBenchmarks.cs`). `benchmarks.yaml`
runs the full suite on every push to `main` and publishes results to the
`gh-pages` trend chart, so a *regression* in bytes-allocated-per-call is
visible over time for every method, even without a hard zero-byte gate.
That's the right level of enforcement here: catch "this got worse," don't
assert "this is exactly zero" for methods that were never meant to be.

## If a future method needs a real zero-alloc guarantee

If a future addition to this library is specifically designed to avoid
allocating (e.g. a pooled-buffer variant), document that method's
allocation contract in its XML doc `<remarks>` and add a targeted
`GC.GetAllocatedBytesForCurrentThread()` assertion scoped to that one
method — don't generalize it back to the whole public surface.
