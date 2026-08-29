#Requires -Version 7.0
<#
.SYNOPSIS
    Compares the built assemblies against the previously published NuGet
    package for binary/ABI compatibility (#232).

.DESCRIPTION
    A1 (PublicApiAnalyzers) catches added/removed *signatures* via
    PublicAPI.Shipped.txt diffs. It does not catch behavioural ABI breaks —
    default-value changes, nullability annotation flips, binary-layout
    shifts — that consumers' already-compiled assemblies trip over at
    runtime. This script closes that gap using Microsoft.DotNet.ApiCompat.Tool.

    Runs once per shipped package (every *.csproj directly under src/) —
    the repo may ship more than one independently-versioned package.

    Per SemVer, a PATCH/MINOR release must not introduce an ABI break; a
    MAJOR release may. Intentional breaks in a MAJOR release are waived via
    compat-suppressions.txt (line-based substring matching against the
    apicompat message, '#'-prefixed lines are comments). The same
    suppressions file is shared across all packages in the repo.

    IMPORTANT: apicompat's own process exit code is non-zero whenever it
    DETECTS a break, even one this script goes on to suppress. This script
    filters that output and must `exit 0` explicitly on its own success
    path — do not let apicompat's raw exit code leak through, and always
    invoke this script with `pwsh -Command`, not `-File`, when reproducing
    a CI failure locally (the leaked exit code only surfaces via -Command).

.PARAMETER Configuration
    Build configuration to compare. Defaults to Release.

.PARAMETER SuppressionsFile
    Path to the suppressions file. Defaults to compat-suppressions.txt at
    the repo root. Exposed as a parameter so a suppression can be proven
    dead by re-running with an empty file.
#>
param(
    [string]$Configuration = 'Release',
    [string]$SuppressionsFile = (Join-Path $PSScriptRoot '..' 'compat-suppressions.txt')
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$srcProjects = Get-ChildItem -Path (Join-Path $repoRoot 'src') -Recurse -Filter '*.csproj'

if ($srcProjects.Count -eq 0) {
    Write-Warning "No src csprojs found — nothing to compare."
    exit 0
}

$suppressionLines = @()
if (Test-Path $SuppressionsFile) {
    $suppressionLines = Get-Content $SuppressionsFile |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -and -not $_.StartsWith('#') }
}
Write-Host "Loaded $($suppressionLines.Count) suppression entr$(if ($suppressionLines.Count -eq 1) {'y'} else {'ies'}) from $SuppressionsFile" -ForegroundColor Cyan

function Get-PreviousVersion {
    param([string]$PackageId, [string]$BelowVersion)

    $indexUrl = "https://api.nuget.org/v3-flatcontainer/$($PackageId.ToLowerInvariant())/index.json"
    try {
        $index = Invoke-RestMethod -Uri $indexUrl -ErrorAction Stop
    } catch {
        Write-Warning "Could not fetch version index for $PackageId — assuming this is the first release. $($_.Exception.Message)"
        return $null
    }

    $target = [version]($BelowVersion -replace '[-+].*$', '')
    $candidates = $index.versions |
        Where-Object { $_ -notmatch '-' } |
        ForEach-Object { [version]$_ } |
        Where-Object { $_ -lt $target } |
        Sort-Object -Descending

    if ($candidates.Count -eq 0) {
        return $null
    }

    return $candidates[0].ToString()
}

dotnet tool install --global Microsoft.DotNet.ApiCompat.Tool --version 10.* 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    # Tool may already be installed from a prior run on a self-hosted/cached runner.
    Write-Host "ApiCompat.Tool install returned non-zero (may already be installed) — continuing." -ForegroundColor Yellow
}

# The install failure above is tolerated only because the tool may already be
# present. Verify it actually resolves before proceeding — otherwise the loop
# below dies later with a raw CommandNotFound that obscures the real problem.
if (-not (Get-Command apicompat -ErrorAction SilentlyContinue)) {
    Write-Error "apicompat was not found on PATH after 'dotnet tool install --global Microsoft.DotNet.ApiCompat.Tool'. Ensure the dotnet global tools directory (e.g. ~/.dotnet/tools) is on PATH and the install succeeded, then re-run."
    exit 1
}

$allUnsuppressedBreaks = @()

foreach ($srcProject in $srcProjects) {
    $packageId = $srcProject.BaseName

    Write-Host "`n==========================================" -ForegroundColor Cyan
    Write-Host "Package: $packageId" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan

    [xml]$projectXml = Get-Content $srcProject.FullName -Raw
    $currentVersionRaw = $projectXml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $currentVersionRaw) {
        Write-Error "Could not read <Version> from $($srcProject.FullName)"
        exit 1
    }
    $currentVersion = $currentVersionRaw.Trim()
    Write-Host "Current version: $currentVersion" -ForegroundColor Cyan

    # TFMs the project currently targets — used to distinguish a deliberately
    # removed TFM (a breaking change) from a merely-not-yet-built one.
    $targetFrameworksRaw = $projectXml.Project.PropertyGroup.TargetFrameworks | Where-Object { $_ } | Select-Object -First 1
    if (-not $targetFrameworksRaw) {
        $targetFrameworksRaw = $projectXml.Project.PropertyGroup.TargetFramework | Where-Object { $_ } | Select-Object -First 1
    }
    $targetFrameworks = @(
        ([string]$targetFrameworksRaw) -split ';' |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_ }
    )

    $previousVersion = Get-PreviousVersion -PackageId $packageId -BelowVersion $currentVersion

    if (-not $previousVersion) {
        Write-Host "No previously published version found below $currentVersion — nothing to compare. Passing." -ForegroundColor Green
        continue
    }

    Write-Host "Comparing against previously published version: $previousVersion" -ForegroundColor Cyan

    # Download the previous package and extract its lib/ assemblies.
    $downloadDir = Join-Path $repoRoot 'apicompat-previous' $packageId
    New-Item -ItemType Directory -Force -Path $downloadDir | Out-Null
    $previousNupkgPath = Join-Path $downloadDir "$($packageId.ToLowerInvariant()).$previousVersion.nupkg"
    $downloadUrl = "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLowerInvariant())/$previousVersion/$($packageId.ToLowerInvariant()).$previousVersion.nupkg"
    Invoke-WebRequest -Uri $downloadUrl -OutFile $previousNupkgPath -UseBasicParsing

    $previousExtractDir = Join-Path $downloadDir 'extracted'
    if (Test-Path $previousExtractDir) {
        Remove-Item -Recurse -Force $previousExtractDir
    }
    Expand-Archive -Path $previousNupkgPath -DestinationPath $previousExtractDir

    $previousLibDlls = Get-ChildItem -Path (Join-Path $previousExtractDir 'lib') -Recurse -Filter "$packageId.dll"

    foreach ($previousDll in $previousLibDlls) {
        $tfm = $previousDll.Directory.Name
        $currentDll = Join-Path $srcProject.DirectoryName 'bin' $Configuration $tfm "$packageId.dll"

        if (-not (Test-Path $currentDll)) {
            if ($targetFrameworks -notcontains $tfm) {
                # The TFM was shipped in the previous package but the csproj no
                # longer targets it at all — that IS a breaking change for
                # consumers on that framework, not something to skip past.
                $breakMessage = "$packageId ($tfm): TargetFramework $tfm was shipped in $previousVersion but is no longer targeted - dropping a TFM is a breaking change"

                $isSuppressed = $false
                foreach ($entry in $suppressionLines) {
                    if ($breakMessage.Contains($entry)) {
                        $isSuppressed = $true
                        break
                    }
                }

                if ($isSuppressed) {
                    Write-Host "  [suppressed] $breakMessage" -ForegroundColor DarkYellow
                } else {
                    Write-Host "  [BREAK] $breakMessage" -ForegroundColor Red
                    $allUnsuppressedBreaks += $breakMessage
                }
                continue
            }

            # TFM is still in <TargetFrameworks> but has no build output — a
            # partial build. Skipping here would silently weaken the gate, so
            # fail hard and tell the caller to build everything first.
            Write-Error "No built assembly for $packageId ($tfm) at $currentDll, but $tfm is still listed in <TargetFrameworks>. Build all TFMs first (dotnet build -c $Configuration) and re-run — a partial build must not weaken the ABI gate."
            exit 1
        }

        Write-Host "`n=== Comparing $packageId ($tfm) ===" -ForegroundColor Cyan
        Write-Host "  left  (previous, $previousVersion): $($previousDll.FullName)"
        Write-Host "  right (current,  $currentVersion): $currentDll"

        $output = & apicompat --left $previousDll.FullName --right $currentDll 2>&1
        $rawExitCode = $LASTEXITCODE

        if ($rawExitCode -eq 0) {
            Write-Host "  No breaks detected for $tfm." -ForegroundColor Green
            continue
        }

        foreach ($line in $output) {
            $isSuppressed = $false
            foreach ($entry in $suppressionLines) {
                # Literal substring match (case-sensitive). -like would treat
                # wildcard chars (* ? [ ]) inside the suppression entry as
                # patterns, silently widening or breaking the match.
                if (([string]$line).Contains($entry)) {
                    $isSuppressed = $true
                    break
                }
            }

            if ($isSuppressed) {
                Write-Host "  [suppressed] $line" -ForegroundColor DarkYellow
            } elseif ($line -match '\S') {
                Write-Host "  [BREAK] $line" -ForegroundColor Red
                $allUnsuppressedBreaks += "$packageId ($tfm): $line"
            }
        }
    }
}

if ($allUnsuppressedBreaks.Count -gt 0) {
    Write-Host "`n==========================================" -ForegroundColor Red
    Write-Host "❌ UNSUPPRESSED ABI BREAKS DETECTED" -ForegroundColor Red
    Write-Host "==========================================" -ForegroundColor Red
    foreach ($b in $allUnsuppressedBreaks) {
        Write-Host "  $b" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "If this is a deliberate MAJOR-version breaking change, add a substring of" -ForegroundColor Yellow
    Write-Host "each message above to compat-suppressions.txt with a comment explaining why." -ForegroundColor Yellow
    exit 1
}

Write-Host "`n✅ No disallowed ABI breaks in any package." -ForegroundColor Green
exit 0
