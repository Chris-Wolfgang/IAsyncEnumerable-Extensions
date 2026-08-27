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
every package under `src/` (`Wolfgang.Extensions.IAsyncEnumerable` and
`Wolfgang.Extensions.IAsyncEnumerable.Polyfill`) on both `ubuntu-latest`
and `windows-latest` and SHA-256-hashes the per-TFM assembly from each.
The hashes must match exactly.

**The assembly, not the `.nupkg`, is the unit being compared.** A
`.nupkg` is a ZIP; ZIP central-directory entries embed per-file
timestamps, so two byte-identical DLLs still produce different `.nupkg`
hashes. Hashing the DLL directly is the actual reproducibility claim.

## Verifying a specific release yourself

Every GitHub Release has a `reproducible-build-manifest.json` asset with a
`packages` array — one entry per shipped package, each listing the
expected SHA-256 hash of its assembly for each target framework — plus the
exact toolchain (.NET SDK version) that produced them all.

1. Install the matching .NET SDK version (see `toolchain.dotnetSdk` in the
   manifest).
2. Clone this repo and check out the release tag:
   ```bash
   git clone https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions.git
   cd IAsyncEnumerable-Extensions
   git checkout <tag>
   ```
3. Build the package you want to verify the same way CI does (substitute
   the package directory name under `src/` for `<packageId>`):
   ```bash
   CI=true dotnet build src/<packageId>/<packageId>.csproj --configuration Release
   ```
4. Hash the assembly for the TFM you care about and compare against that
   package's entry in the manifest:
   ```bash
   sha256sum src/<packageId>/bin/Release/<tfm>/<packageId>.dll
   ```
5. If the hashes don't match, please open an issue with your OS, .NET SDK
   version, the package/TFM, and the hash you got — that's exactly the
   kind of discrepancy this verification exists to catch.

## Third-party attestation

There is no automated third-party attestation pipeline (e.g. via
[reproducible-builds.org](https://reproducible-builds.org/)'s conventions
or [vouchsafe.io](https://vouchsafe.io/)) at this time. If you independently
verify a release, filing an issue with the result (per step 5 above) is
the current process — a discrepancy report either surfaces a real problem
or, if the build turns out not to be reproducible on your platform for a
legitimate reason, documents that limitation for the next person.
