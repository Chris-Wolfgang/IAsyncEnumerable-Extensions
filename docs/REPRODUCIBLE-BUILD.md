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
`Wolfgang.Extensions.IAsyncEnumerable.Legacy`) on both `ubuntu-latest`
and `windows-latest` and SHA-256-hashes each package's per-TFM assembly
(`.dll`) **and** its symbols (`.pdb`). Every hash must match exactly, or
the run fails.

**The guarantee covers assemblies and symbols, not package files.** A
`.nupkg`/`.snupkg` is a ZIP, and NuGet stamps each entry with the pack
time, so two packs of the same commit differ even on one machine. Hashing
the `.dll` and `.pdb` directly is the actual reproducibility claim.

### What makes it hold across operating systems

Two inputs used to differ between Linux and Windows, so every assembly
hashed differently per OS even though its IL was identical:

- **Newlines in SDK-generated sources.** The SDK writes
  `<Project>.AssemblyInfo.cs`, `*.AssemblyAttributes.cs` and
  `*.GlobalUsings.g.cs` with the build machine's newline. The portable PDB
  embeds and hashes every source, and the DLL takes its PDB checksum and
  module version id from it. `Directory.Build.targets` rewrites those files
  to LF before compilation.
- **.NET Framework reference assemblies.** For the `net462` builds, Windows
  compiled against the installed targeting pack and Linux against the NuGet
  package, which are different builds. `Directory.Build.props` makes every
  OS use the `Microsoft.NETFramework.ReferenceAssemblies` package.

If the check fails again, compare the per-TFM `.pdb` documents and
referenced-assembly records first.

## Verifying a specific release yourself

Every GitHub Release has a `reproducible-build-manifest.json` asset with a
`packages` array — one entry per shipped package, each listing the
expected SHA-256 hash of its assembly (`.dll`) for each target framework —
plus the exact toolchain (.NET SDK version) that produced them all. The
manifest lists assemblies only; the `.pdb` is checked by the weekly
workflow above, not recorded in the release manifest.

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
