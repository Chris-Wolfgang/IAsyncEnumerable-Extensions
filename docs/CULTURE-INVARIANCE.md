# Culture invariance

## This library has no culture-sensitive public surface

`IAsyncEnumerableExtensions` has zero call sites that depend on
`CultureInfo.CurrentCulture` — no `ToString()`, no string formatting, no
string comparison, no numeric parsing. Every one of the 9 public methods
(`ChunkAsync`, `DoAsync` × 2, `ForEachAsync` × 2, `IsEmptyAsync`,
`IsNullOrEmptyAsync`, `NoneAsync` × 2) operates on a generic `T` purely
structurally: enumerate the source, invoke a caller-supplied delegate,
batch into arrays, count. None of that touches culture.

## Allowlist of intentionally culture-sensitive methods

**None.** Every public method is culture-invariant by construction, not by
convention — there's no formatting/comparison logic to opt in or out of.

## How this is verified

Rather than leave that as an unverified claim,
`tests/Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit/CultureInvarianceTests.cs`
exercises every public method under `tr-TR` (dotted/dotless I casing trap),
`de-DE` (decimal comma), `ar-SA` (RTL + Hindi-Arabic digit shapes), `ja-JP`
(full-width digits), and `zh-CN` (collation), asserting each produces the
identical result to the culture-invariant baseline. These tests exist to
catch a *future* regression — a contributor adding a culture-sensitive
operation without realizing it — not because today's implementation is at
risk (#240).

If a future method genuinely needs culture-aware behavior (e.g. a
formatting helper), add it to this allowlist with a one-line justification
and give it its own explicit culture-parameter tests rather than relying
on `CurrentCulture`.
