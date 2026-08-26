# ADR-0004: `BannedSymbols.txt` enforces async-first, not just "no blocking calls"

## Status

Accepted

## Context

A library built around `IAsyncEnumerable<T>` is only as good as its
weakest internal shortcut. A single `.Result`, `.GetAwaiter().GetResult()`,
or `Thread.Sleep` slipped into a helper method silently reintroduces
thread-pool starvation and deadlock risk for every consumer, no matter how
carefully the public async surface is designed — and a code reviewer can
miss one call in a large diff far more easily than an automated gate can.

Two enforcement options existed: rely on reviewer diligence (cheap to skip,
easy to regress), or use the Roslyn `BannedApiAnalyzer`'s `BannedSymbols.txt`
mechanism to fail the build on a documented denylist. The denylist approach
was chosen and scoped deliberately wider than "the obviously blocking APIs"
(`Task.Wait`, `.Result`) to also cover `Thread.Sleep`, synchronous
`System.IO.File`/`Stream` members, and `Parallel.For`/`ForEach`/`Invoke` —
APIs that are not deadlock-prone by themselves but represent a
synchronous-first mindset this library exists to move consumers away from.
Each banned entry carries its own reason/alternative text (visible directly
in the analyzer diagnostic), rather than pointing at external docs.

## Decision

`BannedSymbols.txt` is the enforced async-first policy for this repo's own
source, not merely a deadlock-prevention list. New entries are added
whenever a synchronous alternative to an async-capable API would tempt a
future contributor to reach for the easy synchronous path inside this
library's implementation.

## Consequences

- A contributor who reaches for `Thread.Sleep` or `Parallel.ForEach` inside
  `IAsyncEnumerableExtensions.cs` gets a build error with the async
  replacement named inline, instead of a design smell that only a careful
  reviewer would catch.
- The list is necessarily broader than "things that can deadlock," so it
  will occasionally flag an API that would have been *safe* in a specific
  context (e.g. a genuinely CPU-bound, non-blocking use of
  `Parallel.For`). Bulk analysis contexts should file the exception rather
  than remove the entry.
- The list only governs this repository's own implementation code — it says
  nothing about what a *consumer* of the published NuGet package chooses to
  do in their own code.
