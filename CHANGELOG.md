# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- New package `Wolfgang.Extensions.IAsyncEnumerable.Legacy` (net462;netstandard2.0
  only): terminal operator extension methods (`CountAsync`, `AnyAsync` (2 overloads),
  `FirstAsync`, `FirstOrDefaultAsync`, `ToListAsync`) for `IAsyncEnumerable<T>` on
  TFMs where `System.Linq.AsyncEnumerable` isn't available (#124). Scoped to
  exactly the TFMs the BCL doesn't cover, so there's no ambiguous-call risk with
  `System.Linq.AsyncEnumerable` on net8.0+ — the package simply doesn't apply
  there. Independently versioned starting at 0.1.0.

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.5.4] - 2026-08-27

No public API changes — a follow-on maintenance / infrastructure release
closing out the CI/CD hardening backlog: real test coverage on the test
assemblies themselves (found and fixed 3 genuine gaps), the last Stryker
mutation survivors, PackageValidation and NuGet Trusted Publishing gates,
a documented incident-response appendix, and a supply-chain pin sweep
across every workflow.

### Added

- `ChunkAsyncTests`, `DoAsyncTests`, `ForEachAsyncTests`: 5 new tests isolating
  each pre-loop `ThrowIfCancellationRequested()` guard from the matching
  in-loop check, killing the 5 Stryker survivors tracked in #304. The
  existing pre-canceled-token tests drained the whole sequence, so a
  pre-canceled token also tripped the in-loop check on the second
  `MoveNextAsync()` — masking a removed pre-loop check. The new tests assert
  on the very first `MoveNextAsync()`/action invocation instead, so the
  source is provably never touched. Mutation score: 92.06% → 100%, 0
  survivors (#304).
- `IAsyncEnumerable Extensions.slnx.DotSettings`: ReSharper InspectCode
  noise-floor profile, silencing the `RS0016`/`RS0036`/`RS0037`
  (PublicApiAnalyzer) findings InspectCode duplicates from this repo's own
  PublicApiAnalyzer gate. Dropped InspectCode's findings on a clean main
  from 349 to 40, with no change to the error-only merge gate (all 349 were
  already warning-severity). `*.DotSettings` added to `pr.yaml`'s protected
  configuration files, closing the gap where a PR could edit it without
  triggering the maintainer-review guard (#266).
- `EnablePackageValidation` + `PackageValidationBaselineVersion` (0.5.3) on
  the src csproj — protects consumers against an unintentional
  binary-breaking change at the next release. Verified locally: `dotnet
  pack` reports zero compatibility breaks against the published 0.5.3
  baseline (#286).
- `coverlet.runsettings`: `IncludeTestAssembly` was never set (defaults to
  `false`), so the coverage gate only ever measured `src/` — test-code dead
  helpers and unused branches were invisible fleet-wide except on
  ETL-SqlBulkCopy. Also: `Tests.Concurrency`, `Tests.DocExamples`, and
  `Tests.Fuzz` were missing `coverlet.collector` entirely, so their
  coverage collection silently no-op'd even once test-assembly
  instrumentation was on. Fixed both; added 3 tests to
  `DocExampleTests`/`DocExampleSource` covering branches only reachable
  with a synthetic (not real) doc example, and removed one dead
  100,000-iteration safety cap in `DeepBehaviorTests`' `ThrowingAsyncEnumerable`
  fake that no test path could ever reach. Merged line coverage across all
  4 test projects + src: 99.1% (`Tests.Concurrency`/`DocExamples.Source`/
  `DocExamples.Compiler` now 100%). The remaining ~0.9% is exclusively
  delegate bodies a test provably never invokes by design (e.g.
  "empty source executes no actions"), fuzz-property counter-example
  branches only reachable if the code under test were actually broken, and
  compiler-generated async-lambda state-machine bookkeeping — none
  fixable without defeating the test's own purpose (#292).

### Changed

- `release.yaml`: migrated NuGet publishing from a long-lived
  `NUGET_API_KEY` secret to Trusted Publishing (OIDC) via `NuGet/login@v1`
  — every release now exchanges the workflow's GitHub OIDC token for an
  ephemeral (~1-hour) push key, so there's no standing credential to leak,
  phish, or rotate. Matches the other 9 fleet repos already on this
  pattern. **Requires a one-time nuget.org Trusted Publishing policy for
  this repo before the next release** — see the job comment in
  `release.yaml` (#279).

### Deprecated

### Removed

### Fixed

### Security

- Pinned 42 action references that zizmor flagged as unpinned (High
  severity) across 7 workflow files — `benchmarks.yaml`,
  `build-all-versions.yaml`, `codeql.yaml`, `docfx.yaml`, `pr.yaml`,
  `release.yaml`, `stryker.yaml` — reusing the exact commit SHA already
  pinned elsewhere in the repo for each action, for consistency.
  Pre-existing debt, not introduced by this cycle's PRs. Also documented a
  `# zizmor: ignore[dangerous-triggers]` suppression on `pr.yaml`'s
  `pull_request_target` trigger — deliberate and already mitigated by the
  existing trusted-config-fetch + protected-file-change-detection steps
  (see `docs/WORKFLOW_SECURITY.md`), not an oversight. `zizmor`/`actionlint`
  both clean after the fix (0 High findings; the 5 remaining are
  Informational false-positive-shaped: trusted step-outcome interpolation
  and one style suggestion).

- `SECURITY.md`: added a "Release path & compromise scope" appendix
  documenting the OIDC release identity, the (nonexistent) fallback if
  Trusted Publishing is compromised, and the package coordinates for
  unlisting — the load-bearing facts a maintainer would need during an
  incident, without duplicating GitHub's/NuGet's own generic
  incident-response docs (#246).

## [0.5.3] - 2026-08-26

No public API changes. This release is a maintenance / infrastructure
refresh delivering the repo's thorough-review CI/CD hardening campaign —
supply-chain verification, testing depth, and documentation, with zero
source or public-API changes.

### Added

- `cross-platform-differential.yaml` + `scripts/normalize-trx.py`:
  weekly + on-demand. Runs the net10.0 test suite on `ubuntu-latest`,
  `ubuntu-24.04-arm`, `macos-latest` (Apple Silicon), and
  `windows-latest`, normalizes each `.trx` (test name + outcome +
  first line of any failure message; timestamps/durations/machine
  names stripped), and diffs every platform against the linux-x64
  baseline. Distinct from `pr.yaml`'s existing Stage 1/2/3, which only
  verify PASS/FAIL per OS — this verifies they pass the *same way*.
  Scoped to net10.0 only (the one TFM that genuinely runs on all 4
  platforms; net462 is Windows-only). Verified `normalize-trx.py`
  locally against a real `.trx`: 105 tests normalized correctly;
  confirmed identical runs diff clean (exit 0) and a deliberately
  altered outcome is caught (exit 1) (#235).
- `sourcelink-verify.yaml` + `docs/SOURCELINK-VERIFICATION.md`: weekly +
  on-demand verification that the PDB's embedded SourceLink URLs
  actually resolve to real source content. Driving an actual IDE
  debugger through F11 isn't scriptable in CI, so this verifies every
  mechanical prerequisite instead (`sourcelink print-urls` + `curl`
  each URL). Verified locally against a real build: the real URL
  resolves (200, non-empty), and a deliberately invalid commit SHA on
  the same path correctly 404s, confirming the check has teeth (#239).

- `pr-benchmarks.yaml` + `benchmarks/compare-pr-benchmarks.py`: runs the
  BDN suite on the PR's HEAD and on its base branch, comments a
  time/allocation delta table on the PR (idempotently updated, not
  appended), and fails the check on an allocation regression > 50%
  unless the PR carries the new `perf-impact-acknowledged` label. Time
  regressions (> 20%) are reported but advisory-only — shared
  GitHub-hosted runner wall-clock is too noisy to hard-fail on.
  Forward-looking, distinct from the existing `benchmarks.yaml` (P2),
  which graphs the trend on every push to `main` after the fact (#249).
- `tests/Wolfgang.Extensions.IAsyncEnumerable.Tests.Fuzz`: continuous
  fuzz testing via CsCheck (chosen over FsCheck — .NET-native idiomatic
  API, no F# interop). 4 properties: `ChunkAsync` concatenation,
  `DoAsync` transparency, `NoneAsync(predicate)` ↔ `!Any(predicate)`,
  `IsEmptyAsync` ↔ `count == 0`. `fuzz.yaml` runs weekly at 100,000
  cases / 300s per property and files an issue on failure (default
  locally/ad hoc: 1,000 cases, no time limit, via
  `FUZZ_ITERATIONS`/`FUZZ_TIME_SECONDS`). Reproduction uses CsCheck's
  own seed mechanism (`CsCheck_Seed=...`, printed in the failure
  message) rather than a separate replay-folder artifact.

  Verified locally: all 4 properties pass at 1,000 and 20,000
  iterations. Verified the gate actually fails and reports a
  reproducible seed — temporarily broke one property's assertion,
  confirmed `CsCheckException` with a seed, reverted.
- `reproducible-build.yaml`: weekly + on-demand verification that
  building the same commit on `ubuntu-latest` and `windows-latest`
  produces a byte-identical `Wolfgang.Extensions.IAsyncEnumerable.dll`
  per TFM (SHA-256 compared, assembly not `.nupkg` — ZIP timestamps
  make the package hash differ even when the DLL doesn't). Verified
  locally that the hash step runs cleanly against the real Release
  build.
- `release.yaml`: emits `reproducible-build-manifest.json` (per-TFM
  SHA-256 + toolchain) attached to every GitHub Release, so a consumer
  can independently verify their own build matches. Verified locally
  end-to-end against the real build output.
- `docs/REPRODUCIBLE-BUILD.md`: documents the deterministic-vs-
  reproducible distinction, the third-party verification procedure,
  and links from README's "Verify the build" (#241, #250).
- `docs/CULTURE-INVARIANCE.md` + `CultureInvarianceTests.cs`: documents
  that the library has zero culture-sensitive public surface (no
  `ToString()`, formatting, comparison, or parsing) and empirically
  proves it — every public method exercised under `tr-TR`/`de-DE`/
  `ar-SA`/`ja-JP`/`zh-CN`, asserting identical results to the
  culture-invariant baseline. Verified locally: all 25 cases (5
  methods × 5 cultures) pass across every targeted TFM
  (net462 → net10.0) (#240).
- `samples/ShadowWorkloads`: 4 realistic consumer scenarios doubling as
  usage documentation — a paginated-SQL bulk-insert pipeline
  (`ChunkAsync`), an HTTP-paged API with a telemetry side-effect
  (`DoAsync` + `ForEachAsync`), a cancellable file-line stream
  (`ForEachAsync` + `CancellationToken`), and concurrent independent
  consumers (`Task.WhenAll` + `ChunkAsync`). `shadow.yaml` runs them
  nightly + on demand and gates on allocation regression (`compare.py`,
  >50% fails; latency is advisory only — shared runner wall-clock is
  too noisy to hard-fail on). `docs/shadow-baseline.json` is the
  committed baseline, captured locally. Verified end-to-end: ran all 4
  scenarios, captured real numbers, confirmed `compare.py` passes
  against the matching baseline and fails against a deliberately
  corrupted one (#229).
- `license-audit.yaml` + `licenses/allowed-licenses.json`: gates the
  src/ project's shipped dependency graph (analyzer packages excluded —
  build-time only, never distributed) against an MIT/Apache-2.0/
  BSD-2/BSD-3/ISC/0BSD allowlist on every PR touching a `.csproj`, plus
  weekly. `THIRD-PARTY-NOTICES.md` documents the current baseline (one
  dependency: `Microsoft.Bcl.AsyncInterfaces` 10.0.11, MIT) and now
  ships in the NuGet package alongside `README.md`. Verified locally:
  the gate passes against the real allowlist and fails (non-zero exit)
  against a deliberately restrictive one (#243).
- `scripts/Check-ApiCompatibility.ps1` + `compat-suppressions.txt`:
  release-time ABI-compatibility gate via `Microsoft.DotNet.ApiCompat.Tool`,
  comparing each built TFM's assembly against the previously-published
  NuGet version. Catches behavioural ABI breaks (default-value changes,
  nullability flips, binary-layout shifts) that PublicAPI.Shipped.txt
  diffs don't. Wired into `release.yaml`'s `pack-and-validate` job.
  Verified locally end-to-end against the real published 0.5.1→0.5.2
  history — all 4 TFMs compared, zero breaks (#232).
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

- `stryker-config.json`: mutation-score gate is now real —
  `thresholds.break` raised from `0` to `85` (was a report-only bucket
  before). Added `ignore-mutations: ["string"]` +
  `ignore-methods: ["ConfigureAwait"]` to drop accepted-equivalent
  mutants (exception-message text, `ConfigureAwait(false)→(true)`) from
  the denominator — measured score went from 73.86% (raw) to 92.06%
  (filtered) on the current test suite. `stryker.yaml` now also runs on
  every PR touching `src/**`/`tests/**` (plain `pull_request`, not
  folded into `pr.yaml`'s gated pipeline — read-only measurement, no
  elevated permissions needed) in addition to the existing weekly
  schedule + dispatch; ~1.5 min per run on this repo's single-file
  source, cheap enough for real per-PR gating even though the fleet
  convention keeps Stryker schedule-only elsewhere. Filed #304 for the
  5 remaining real survivors (pre-cancelled-token statement mutants,
  not equivalent — genuine test-coverage gaps, out of scope here) (#231).

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

[Unreleased]: https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/compare/v0.5.4...HEAD
[0.5.4]: https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/compare/v0.5.3...v0.5.4
[0.5.3]: https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/compare/v0.5.2...v0.5.3
[0.5.2]: https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/compare/v0.5.1...v0.5.2
[0.5.1]: https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/compare/v0.5.0...v0.5.1
