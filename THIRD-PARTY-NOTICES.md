# Third-Party Notices

This repo ships two packages, each with one runtime dependency.
`license-audit.yaml` gates new dependencies against
`licenses/allowed-licenses.json` on every PR that touches a `.csproj`;
regenerate this file's tables by hand (from `dotnet-project-licenses`'s
console output — see the commands below) and commit it whenever the
dependency graph changes.

## Wolfgang.Extensions.IAsyncEnumerable

| Package | Version | License |
|---------|---------|---------|
| [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) | 10.0.11 | [MIT](https://licenses.nuget.org/MIT) |

> Microsoft.Bcl.AsyncInterfaces provides the `IAsyncEnumerable<T>` /
> `IAsyncDisposable` interfaces for netstandard2.0/net462; it is not pulled
> in on net8.0+, where these types are part of the framework.

## Wolfgang.Extensions.IAsyncEnumerable.Polyfill

| Package | Version | License |
|---------|---------|---------|
| [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) | 10.0.11 | [MIT](https://licenses.nuget.org/MIT) |

> Same dependency, same reason — this package targets only
> net462/netstandard2.0, so it always needs it.

## Copyright

Microsoft.Bcl.AsyncInterfaces — © Microsoft Corporation. All rights reserved.

## Baseline scan

Generated from:
```
dotnet-project-licenses --input src/Wolfgang.Extensions.IAsyncEnumerable/Wolfgang.Extensions.IAsyncEnumerable.csproj
dotnet-project-licenses --input src/Wolfgang.Extensions.IAsyncEnumerable.Polyfill/Wolfgang.Extensions.IAsyncEnumerable.Polyfill.csproj
```
against each src project's shipped (non-analyzer, non-test) dependency
graph. Analyzer packages (`Roslynator.Analyzers`, `Meziantou.Analyzer`,
`SonarAnalyzer.CSharp`, etc.) are build-time only (`PrivateAssets=all`) and
are not distributed in either NuGet package, so they're intentionally
excluded from this audit.
