# ADR-0005: Legacy package conventions (TFM scope, ValueTask returns, derived AssemblyVersion)

## Status

Accepted

## Context

Issue #124 asked for terminal operators (`CountAsync`, `AnyAsync`,
`FirstAsync`, `FirstOrDefaultAsync`, `ToListAsync`) for consumers on TFMs
where `System.Linq.AsyncEnumerable` is not available. Shipping them from the
main package would either collide with the BCL's methods on net8.0+
(ambiguous-call errors for anyone with `using System.Linq;`) or require
`#if` guards that make the shipped surface differ per TFM.

The new `Wolfgang.Extensions.IAsyncEnumerable.Legacy` package makes three
deliberate choices that differ from the main package, recorded here so the
asymmetry reads as decision, not drift.

## Decision

1. **TFM scope: `net462;netstandard2.0` only.** The package exists solely
   for TFMs the BCL doesn't cover. It has no net8.0+ target at all, which
   eliminates the ambiguity problem structurally — no `#if` guards, no
   separate namespace, no per-TFM surface differences. On modern TFMs the
   package simply cannot be referenced into ambiguity.

2. **Return type: `ValueTask<T>` (not `Task<T>`).** Matches the BCL
   `System.Linq.AsyncEnumerable` signatures these methods polyfill, so
   consumer code written against this package ports to the BCL methods
   without signature changes when the consumer eventually retargets to
   net8.0+. The main package's `Task`-returning methods predate this
   decision and are shipped public API — they keep `Task` for binary
   compatibility. The two conventions coexist deliberately: main = its own
   API family, Legacy = BCL-mirroring polyfill.

3. **AssemblyVersion: derived `0.{Minor}.0.0` during 0.x (not the ADR-0001
   pin).** ADR-0001's fixed pin is the right call for the main package,
   whose pin shipped at `1.0.0.0` before this distinction existed. For a
   NEW net462-targeting library still in 0.x, the fleet policy is a derived
   `0.{Minor}.0.0`: pre-1.0, SemVer's breaking position is the MINOR digit,
   so a breaking 0.x release changes netfx binding identity and gives
   strong-name consumers a load-time signal instead of a silent
   `MissingMethodException` — and it keeps the `1.0.0.0` identity unspent
   until the real 1.0. At 1.0 the package switches to a fixed
   `Major.0.0.0` pin and converges with ADR-0001's scheme.

4. **Overload scope: exactly issue #124's surface.** `AnyAsync` gets a
   predicate overload (it was in the issue's proposed API); `CountAsync`,
   `FirstAsync`, and `FirstOrDefaultAsync` do not, although the BCL has
   predicate forms of all of them. Deliberate minimalism for the first
   release: predicate overloads are additive, so they can ship in any later
   MINOR release if demand shows up. Consumers can compose today via
   `source.AnyAsync(predicate)` or a filtering wrapper.

## Consequences

- The repo permanently ships two return-type conventions across its two
  packages. Anyone extending the main package uses `Task`; anyone extending
  Legacy uses `ValueTask` to stay BCL-parallel.
- ADR-0001 is scoped by this ADR: its pin applies to the main package (and
  to any package that has reached 1.0), not to new pre-1.0 packages.
- When Legacy reaches 1.0, its csproj must switch from the derived
  expression to a fixed `1.0.0.0` pin — a conscious, reviewed step, exactly
  like ADR-0001's MAJOR-bump rule.
