# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `tests/Wolfgang.Extensions.IAsyncEnumerable.Tests.DocExamples`: compiles
  every XML-doc `<example><code>` block (8 total) against the real
  assembly inside a Roslyn-hosted neutral-context harness, so a
  renamed/removed member the docs still reference fails the build
  instead of drifting silently. Verified the guard actually fires:
  temporarily renamed a method in one example, confirmed `CS1061` at
  the `#line`-mapped real doc location, reverted (#237).
- `tests/Wolfgang.Extensions.IAsyncEnumerable.Tests.Concurrency`: 5
  `STRESS_ITERATIONS`-scaled stress tests running many independent
  consumers concurrently over `ChunkAsync`/`DoAsync`/`ForEachAsync`/
  `NoneAsync` and racing `DisposeAsync` across independent enumerators,
  asserting correctness-under-contention rather than just timing.
  `concurrency.yaml` runs it weekly (5000 rounds) + on-demand. Coyote
  was evaluated and skipped (rough `IAsyncEnumerable` support, net8.0-only
  CLI) — this library also has no shared mutable state to model-check in
  the first place (#233).
- `docs/ALLOCATION-POLICY.md`: documents that no public method is
  zero-alloc by design (every method is an async/iterator state
  machine, and `ChunkAsync`'s array allocation is its whole point), and
  points to the existing `[MemoryDiagnoser]` BDN trend as the ongoing
  allocation-regression signal instead of a half-implemented zero-byte
  gate (#242).
- `tests/AotSmoke`: Native AOT / trim compatibility smoke test. A console
  consumer exercises every public method on `IAsyncEnumerableExtensions`
  and asserts real results; `aot-smoke.yaml` publishes it
  `PublishAot`+`PublishTrimmed` on linux-x64 and runs it, so a trimmed
  member fails the check instead of silently no-opping. This library has
  no reflection or `Expression.Compile`, so no trim-safety annotations
  were needed (#238).
- `docs/adr/` — Architecture Decision Records: `TEMPLATE.md`, `index.md`,
  and four retroactive ADRs covering the AssemblyVersion pin, the two
  `DoAsync` overloads, `ChunkAsync`'s `ICollection<T>` return type, and
  the `BannedSymbols.txt` async-first enforcement policy (#245).
- `docs/migrations/TEMPLATE-major-version-migration.md` establishing the
  migration-guide convention for future major-version releases (#244).

### Changed

### Deprecated

### Removed

### Fixed

### Security

- `release.yaml`: SLSA build-provenance attestation via
  `actions/attest-build-provenance`, binding each published `.nupkg` to
  the exact workflow run/commit/repo that produced it — closes the
  supply-chain-hardening loop alongside the CycloneDX SBOM generation
  already in place. `SECURITY.md` documents `gh attestation verify` for
  consumers. Package signing (a third, complementary layer) stays
  tracked separately in #289, blocked on a code-signing certificate
  (#234).
- `workflow-security.yaml`: zizmor + actionlint run on any PR/push touching
  `.github/workflows/**`, catching workflow-level vulnerabilities (untrusted
  `run:`-block injection, missing `permissions:`, unpinned actions) that
  CodeQL doesn't inspect. zizmor findings upload as SARIF to the Security
  tab; actionlint posts inline PR review comments and fails on `error`.
  `.zizmor.yml` holds the repo-wide suppression baseline. Documented in
  `docs/WORKFLOW_SECURITY.md` (#248).
- `scorecard.yaml`: weekly + push-to-main OSSF Scorecard scan, SARIF
  uploaded to the Security tab, badge added to `README.md`, 7.5 score
  floor documented in `SECURITY.md` (#247).

## [0.5.2] - 2026-07-06

### Changed

- Dependabot bump: dotnet-dependencies group (2 packages).
## [0.5.1] - 2026-06-06

No public API changes. This release is a maintenance / infrastructure refresh
plus a fix for a Release-build blocker.

### Added

- `PublicAPI.Shipped.txt` + `PublicAPI.Unshipped.txt` and the
  `Microsoft.CodeAnalysis.PublicApiAnalyzers` package, so any future
  breaking change to the public surface fails the build instead of
  silently shipping (A1, #199).
- `DeepBehaviorTests` covering concurrent-disposal failure modes,
  `AsyncLocal` flow correctness across `await` boundaries, and
  deterministic property-based fuzz tests for `ChunkAsync`, `DoAsync`,
  `NoneAsync`, and `IsEmptyAsync` (#181, batch 2).
- BenchmarkDotNet baseline benchmarks for the full public API surface
  (`ChunkAsync`, `DoAsync` × 2, `ForEachAsync` × 2, `IsEmptyAsync`,
  `IsNullOrEmptyAsync`, `NoneAsync` × 2) plus a `BenchmarkSwitcher`
  entry point (P1, P2).
- `benchmarks.yaml` workflow that runs the BDN suite on every push to
  `main` and publishes results to `gh-pages` for trend tracking (P2).
- `stryker.yaml` workflow + `stryker-config.json` for nightly mutation
  testing (T3, #187).
- `CHANGELOG.md` following Keep a Changelog conventions (D3, #189).
- Per-PR code-coverage report published to `docs/coverage/` (T1, #185).
- `.github/ISSUE_TEMPLATE/maintenance-task.yaml` for the fleet
  maintenance backlog.
- Curated `benchmarks/.editorconfig` and `tests/.editorconfig` so
  benchmark/test projects are held to the right warning bar without
  separate `Directory.Build.props` overrides (#206).
- Canonical `Directory.Build.props` package metadata (Authors,
  Copyright, RepositoryType, EmbedUntrackedSources, IncludeSymbols,
  ContinuousIntegrationBuild) + SourceLink + `.snupkg` symbol packages
  (CI3).
- `Fix-BranchRuleset.ps1` for ruleset-required-checks fan-out.
- `Validate-DocsDeploy.sh` for local verification of docfx output.

### Changed

- README standardized to the canonical structure and audited for
  accuracy against current code (D2, #188, #191).
- CONTRIBUTING.md and other Markdown files audited for accuracy
  against current code (#192).
- `<Nullable>enable</Nullable>` hoisted from per-project csprojs to
  `Directory.Build.props`, scoped to SDK-style C# projects so legacy
  / F# / VB projects are unaffected.
- Subtree `Directory.Build.props` overrides (tests, benchmarks,
  examples) removed; subdirectory `.editorconfig` files now express
  the per-tree warning policy instead.
- `pr.yaml` Stage 1/2/3 coverage parsers tightened: greedy regex for
  Stage 2 (no longer mis-reads 100% as 0%), decimal-percent parsing
  in Stage 1, zero-match detection in both, and Stage 3 short-circuits
  cleanly when no coverage files exist.
- `docfx.yaml` reworked: pin coverage to a single TFM (net10.0), pass
  `--no-restore` to `dotnet test`, idempotent ReportGenerator install,
  D6 versions.json preservation guard, D8 verify-docs-build step
  before publishing.
- `release.yaml` attaches `.snupkg` symbol packages alongside `.nupkg`.
- BenchmarkDotNet entry point switched from `BenchmarkRunner` to
  `BenchmarkSwitcher` so single benchmarks can be invoked from CI.

### Removed

- Vestigial one-time setup scripts (`scripts/setup.ps1`,
  `scripts/Setup-GitHubPages.ps1`) — post-bootstrap cleanup.
- Root-level `README-FORMATTING.md` (moved under `docs/`, D7).
- Per-project NuGet metadata defaults duplicated from
  `Directory.Build.props`.
- Stale `<AssemblyVersion>1.0.0</AssemblyVersion>` left over from
  initial scaffold; replaced with an explicit `1.0.0.0` binding-
  stability baseline plus a comment explaining the policy (C4).
- Year-specific `<Copyright>` overrides in src csprojs (canonical
  copyright string lives in `Directory.Build.props`).

### Fixed

- **#223 (release blocker)**: `PublicAPI.Shipped.txt` entries did not
  match the canonical signature format the analyzer generates
  internally, causing every cold Release build to fail with
  9 × RS0017 errors per TFM (36 errors total). Two format
  mismatches were responsible:
  `token = default` → `token = default(System.Threading.CancellationToken)`
  (8 entries), and `IAsyncEnumerable<T>? source` →
  `IAsyncEnumerable<T> source` on `IsNullOrEmptyAsync` (the analyzer's
  bundled Roslyn strips the `?` marker on unconstrained `T`).
- XML doc clarifications surfaced by a full code-review pass:
  removed dead `InternalsVisibleTo` attributes, DRYed
  `IsNullOrEmptyAsync` to delegate to `IsEmptyAsync`, fixed
  `NoneAsync`'s no-predicate `<returns>` phrasing, replaced
  `Array.Resize` with explicit `new T[index] + Array.Copy` in
  `ChunkCoreAsync` (#181, batch 1).
- Concrete test-coverage gaps closed for the public API surface
  (T4, #208).

### Security

- CodeQL `security-extended` query pack enabled (S1, #196).
- GitHub Actions hardening: pinned action versions across all
  workflows (S2, #197).
- Trusted-config-fetch pattern in `pr.yaml` so PRs cannot weaken
  their own CI by mutating `.editorconfig` /
  `Directory.Build.props` / `BannedSymbols.txt` / workflows
  without an explicit maintainer admin-bypass.
- `persist-credentials: false` on the gitleaks / stryker checkouts.

[Unreleased]: https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/compare/v0.5.2...HEAD
[0.5.2]: https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/compare/v0.5.1...v0.5.2
[0.5.1]: https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/compare/v0.5.0...v0.5.1
