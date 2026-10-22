# Third-Party Notices

This repo ships two packages, each with one runtime dependency.
`license-audit.yaml` gates new dependencies against
`.github/license-audit/allowed-licenses.json` on every PR that touches a
`.csproj`.

This root file is an informational summary for readers of the repository; it
is not packed. The notices that ship inside each package are generated at
release time by `scripts/third-party-notices.ps1` (`nuget-license`, from
`.config/dotnet-tools.json`) into each project's `obj/THIRD-PARTY-NOTICES.md`,
from that project's own dependency closure. Update the tables below when the
dependency graph changes.

## Wolfgang.Extensions.IAsyncEnumerable

| Package | Version | License |
|---------|---------|---------|
| [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) | 10.0.13 | [MIT](https://licenses.nuget.org/MIT) |

> Microsoft.Bcl.AsyncInterfaces provides the `IAsyncEnumerable<T>` /
> `IAsyncDisposable` interfaces for netstandard2.0/net462; it is not pulled
> in on net8.0+, where these types are part of the framework.

## Wolfgang.Extensions.IAsyncEnumerable.Legacy

| Package | Version | License |
|---------|---------|---------|
| [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) | 10.0.13 | [MIT](https://licenses.nuget.org/MIT) |

> Same dependency, same reason — this package targets only
> net462/netstandard2.0, so it always needs it.

## Copyright

Microsoft.Bcl.AsyncInterfaces — © Microsoft Corporation. All rights reserved.

## Generating the shipped notices

```
dotnet restore
pwsh scripts/third-party-notices.ps1
```
writes `obj/THIRD-PARTY-NOTICES.md` for each src project from its full
transitive dependency closure (`release.yaml` runs it between restore and
pack). Packages referenced with `PrivateAssets="all"` (the analyzers such as
`Roslynator.Analyzers`, `Meziantou.Analyzer`, `SonarAnalyzer.CSharp`, and
SourceLink) are build-time only and not distributed in either NuGet package,
so they are left out.
