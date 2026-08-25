# Third-Party Notices

This package ships one runtime dependency. `license-audit.yaml` gates new
dependencies against `licenses/allowed-licenses.json` on every PR that
touches a `.csproj`; regenerate this file's table by hand (from
`dotnet-project-licenses`'s console output — see the command below) and
commit it whenever the dependency graph changes.

| Package | Version | License |
|---------|---------|---------|
| [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) | 10.0.11 | [MIT](https://licenses.nuget.org/MIT) |

> Microsoft.Bcl.AsyncInterfaces provides the `IAsyncEnumerable<T>` /
> `IAsyncDisposable` interfaces for netstandard2.0/net462; it is not pulled
> in on net8.0+, where these types are part of the framework.

## Copyright

Microsoft.Bcl.AsyncInterfaces — © Microsoft Corporation. All rights reserved.

## Baseline scan

Generated from `dotnet-project-licenses --input src/Wolfgang.Extensions.IAsyncEnumerable/Wolfgang.Extensions.IAsyncEnumerable.csproj`
against the src project's shipped (non-analyzer, non-test) dependency graph.
Analyzer packages (`Roslynator.Analyzers`, `Meziantou.Analyzer`,
`SonarAnalyzer.CSharp`, etc.) are build-time only (`PrivateAssets=all`) and
are not distributed in the NuGet package, so they're intentionally excluded
from this audit.
