# Migrating from vX to vY

> Copy this file to `docs/migrations/vX-to-vY.md` when preparing a major-version
> release, fill in the sections below, and link it from the GitHub Release notes.
> Write it during release prep — retrofitting a migration guide after a major
> version has already shipped is far more expensive than writing it alongside
> the breaking changes.

## Summary

One or two sentences: what changed and why a major bump was required.

## Breaking-change inventory

| API | Change | Replacement |
|-----|--------|--------------|
| `OldMethod(...)` | Removed | `NewMethod(...)` |
| `SomeType.Property` | Renamed to `NewProperty` | `SomeType.NewProperty` |
| `SomeMethod(int)` | Behavior change: now throws on negative input | Validate input before calling, or catch `ArgumentOutOfRangeException` |

Every entry in `PublicAPI.Shipped.txt` removed or changed between the last
minor of vX and the first release of vY belongs in this table.

## Before / after code samples

```csharp
// vX
var result = await source.OldMethod(x => x.Value);

// vY
var result = await source.NewMethod(x => x.Value);
```

Add one before/after pair per breaking change in the inventory above.

## Deprecation timeline

If the old API was marked `[Obsolete]` ahead of removal, note when:

- vX.Y: `[Obsolete]` warning added, old API still functional
- vY.0: old API removed

## Getting help

Open an issue at
https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/issues if a
migration path isn't covered here.
