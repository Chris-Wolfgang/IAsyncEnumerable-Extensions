# SourceLink verification

## What `sourcelink-verify.yaml` actually checks

Debug step-into (a consumer sets a breakpoint in their own code, calls into
this library, presses F11) depends on a chain of independently-verifiable
links: the `.snupkg` uploaded to the NuGet symbol server, a SourceLink JSON
blob embedded in the PDB, and that blob's URLs actually resolving to real
source content on GitHub.

**Driving an actual IDE debugger through F11 in CI isn't scriptable** — there's
no headless way to simulate a developer's keypress and observe whether the
debugger resolved real source vs. decompiled assembly. So this workflow
verifies every *mechanical prerequisite* step-into depends on instead:

1. Build the library (Release). The workflow matrixes over both shipped
   packages: `Wolfgang.Extensions.IAsyncEnumerable` at net10.0 and
   `Wolfgang.Extensions.IAsyncEnumerable.Legacy` at netstandard2.0 (its
   net462 slot doesn't build reliably on ubuntu-latest, and the
   netstandard2.0 PDB embeds the same SourceLink metadata).
2. Use the [`sourcelink`](https://github.com/dotnet/sourcelink) dotnet tool's
   `print-urls` command to read the PDB's embedded SourceLink JSON and list
   every source file's mapped URL (a commit-pinned
   `raw.githubusercontent.com` URL).
3. Fetch every one of those URLs and confirm it resolves with HTTP 200 and a
   non-empty body.

If step 3 fails for any file, a consumer's F11 on that file would silently
fall back to decompiled assembly instead of stepping into real source — the
exact failure mode this exists to catch, without needing to drive an IDE.

## What this does NOT verify

- That a specific IDE (Visual Studio, Rider, VS Code) actually performs the
  fetch-and-display correctly — that's the IDE's own SourceLink client
  implementation, out of this repo's control.
- The `.snupkg` symbol-server fetch path specifically (this checks the PDB
  produced by a local build, which has the same embedded SourceLink data as
  what ships in the `.snupkg`, but doesn't download the `.snupkg` from
  NuGet's symbol server to prove that specific transport works).

## Verified locally (2026-08-25)

Ran the exact `sourcelink print-urls` + `curl` sequence this workflow uses,
against a real Release build:

- Extracted URL: `https://raw.githubusercontent.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/<commit>/src/Wolfgang.Extensions.IAsyncEnumerable/IAsyncEnumerableExtensions.cs`
- Resolved: HTTP 200, non-empty body — confirmed.
- Failure path confirmed separately: an invalid all-zero commit SHA on the
  same path returns HTTP 404, proving the check would actually catch a
  broken mapping.
