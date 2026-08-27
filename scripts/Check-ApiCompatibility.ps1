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
            Write-Warning "No built assembly found for $tfm at $currentDll — skipping (build all TFMs before running this script)."
            continue
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
                if ($line -like "*$entry*") {
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
