# ADR-0001: Pin AssemblyVersion to a fixed 1.0.0.0 baseline

## Status

Accepted

## Context

.NET distinguishes `AssemblyVersion` (the strong-name identity the CLR uses
for binding at load time) from `FileVersion` / `InformationalVersion` (the
human-facing release version, e.g. the NuGet `<Version>`). If
`AssemblyVersion` tracks the NuGet version 1:1, every minor or patch release
changes the assembly's binding identity. A consumer that references the
library directly (not solely through a `PackageReference` that floats to the
latest resolvable version) would need a binding redirect — or a recompile —
on every single-digit bump, even though nothing about the public API
actually broke.

Wolfgang.Extensions.IAsyncEnumerable ships frequent, additive minor/patch
releases (new extension methods, bug fixes). Forcing consumers to react to
binding-identity churn on every one of those releases would defeat the
purpose of semantic versioning: MINOR and PATCH bumps are supposed to be
safe, no-action-required upgrades.

## Decision

`AssemblyVersion` is pinned at `1.0.0.0` and only bumped on a deliberate
breaking API change (i.e., in step with a MAJOR version bump). `FileVersion`
and `InformationalVersion` continue to track the real `<Version>` from the
csproj on every release, so tooling that inspects the physical DLL (Explorer
properties, `dotnet --info`-style diagnostics, crash dumps) still shows the
precise shipped version.

## Consequences

- Consumers referencing the assembly directly never need a binding redirect
  for a MINOR/PATCH release — only for a MAJOR one, which is exactly when a
  breaking change (and therefore a recompile) is expected anyway.
- Two different NuGet package versions (e.g. 0.5.1 and 0.5.2) produce
  assemblies with the *same* `AssemblyVersion`. Tooling that assumes
  `AssemblyVersion` uniquely identifies a release (rather than `FileVersion`)
  will see them as identical — this is intentional, not a bug.
- The `AssemblyVersion` bump must be a conscious, reviewed step tied to a
  MAJOR release, not something automated by the same mechanism that bumps
  `<Version>` — a missed bump on an actual breaking change would silently
  reintroduce the binding-conflict problem this ADR exists to avoid.
