# Reproducible builds

## What "reproducible" means here

**Deterministic** means the same commit, built twice on the *same*
machine/toolchain, produces byte-identical output — the .NET SDK does this
by default for Release builds. **Reproducible** is the stronger claim this
document is about: the same commit, built on a *different* machine/OS,
still produces a byte-identical assembly. That requires
`ContinuousIntegrationBuild` (strips machine-specific paths/timestamps,
enabled here whenever `CI=true`, which GitHub Actions sets automatically)
and [SourceLink](https://github.com/dotnet/sourcelink) (embeds commit
provenance instead of a local file path).

`reproducible-build.yaml` verifies this weekly (+ on demand): it builds
`Wolfgang.Extensions.IAsyncEnumerable` on both `ubuntu-latest` and
`windows-latest` and SHA-256-hashes the per-TFM assembly from each. The
hashes must match exactly.

**The assembly, not the `.nupkg`, is the unit being compared.** A
`.nupkg` is a ZIP; ZIP central-directory entries embed per-file
timestamps, so two byte-identical DLLs still produce different `.nupkg`
hashes. Hashing the DLL directly is the actual reproducibility claim.

## Verifying a specific release yourself

Every GitHub Release has a `reproducible-build-manifest.json` asset
listing the expected SHA-256 hash of the shipped assembly for each target
framework, plus the exact toolchain (.NET SDK version) that produced it.

1. Install the matching .NET SDK version (see `dotnetSdk` in the manifest).
2. Clone this repo and check out the release tag:
   ```bash
   git clone https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions.git
   cd IAsyncEnumerable-Extensions
   git checkout <tag>
   ```
3. Build the same way CI does:
   ```bash
   CI=true dotnet build src/Wolfgang.Extensions.IAsyncEnumerable/Wolfgang.Extensions.IAsyncEnumerable.csproj --configuration Release
   ```
4. Hash the assembly for the TFM you care about and compare against the
   manifest:
   ```bash
   sha256sum src/Wolfgang.Extensions.IAsyncEnumerable/bin/Release/<tfm>/Wolfgang.Extensions.IAsyncEnumerable.dll
   ```
5. If the hashes don't match, please open an issue with your OS, .NET SDK
   version, and the hash you got — that's exactly the kind of discrepancy
   this verification exists to catch.

## Third-party attestation

There is no automated third-party attestation pipeline (e.g. via
[reproducible-builds.org](https://reproducible-builds.org/)'s conventions
or [vouchsafe.io](https://vouchsafe.io/)) at this time. If you independently
verify a release, filing an issue with the result (per step 5 above) is
the current process — a discrepancy report either surfaces a real problem
or, if the build turns out not to be reproducible on your platform for a
legitimate reason, documents that limitation for the next person.
