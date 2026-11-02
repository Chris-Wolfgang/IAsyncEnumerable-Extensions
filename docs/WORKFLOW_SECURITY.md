# Workflow Security

## Overview

This document describes how the GitHub Actions workflows in this repository keep a pull request from
weakening the checks it is validated against. Two workflows share the job:

- `.github/workflows/pr.yaml` builds and tests the pull request **exactly as it is**, including the PR's
  own copies of the workflow and of every analyzer and build-configuration file.
- `.github/workflows/protected-files.yaml` is a guard that fails any pull request mixing a protected
  configuration file with other changes, so a PR cannot loosen a gate and pass it in the same change.

## Security Architecture

### 1. PR validation runs on `pull_request`

**Mechanism**: `pull_request` trigger with a read-only token

`pr.yaml` runs on `pull_request` (plus `push` to `main` and `merge_group`):

```yaml
on:
  pull_request:
    branches:
      - main
  push:
    branches:
      - main
  merge_group:
```

This means:
- The PR's own code, its own copy of `pr.yaml`, and its own copies of `.editorconfig`,
  `Directory.Build.props`, `BannedSymbols.txt` and the other configuration files are what get built and
  tested. There is no step that fetches or overwrites configuration from `main`.
- GitHub hands a `pull_request` run a read-only `GITHUB_TOKEN` and, for forks, no secrets. Nothing in
  `pr.yaml` uses a secret.
- A PR could therefore edit the configuration it is checked against. That is what the protected-files
  guard (below) is for.

### 2. Protected configuration files

**Mechanism**: `protected-files.yaml`, a `pull_request_target` workflow that never checks out PR content

`protected-files.yaml` is the only workflow that runs on `pull_request_target`, and it may because it
never checks out or executes anything from the pull request. It runs from `main` (so a PR cannot edit
the copy that judges it), lists the PR's changed files through the REST API, and classifies the PR:

| The PR changes | Result |
|---|---|
| No protected file | Pass; `pr.yaml` validated everything |
| Only protected files | Pass with a notice; a maintainer reviews the diff on its own |
| Protected files **and** anything else | **Fail**; split the PR |

Dependabot pull requests are exempt (its bumps to `Directory.Build.props` are legitimate). A
`merge_group` job reports the same required check as a no-op so the merge queue does not stall.

**Protected files** (the authoritative list is the `case` statement in `protected-files.yaml`; keep this
table in step with it):

- `.editorconfig` and any `*.editorconfig`
- `Directory.Build.props`, `Directory.Build.targets`
- `BannedSymbols.txt`
- `*.globalconfig`, `*.ruleset`, `*.DotSettings` (matched case-insensitively)
- `coverlet.runsettings`
- `.config/dotnet-tools.json`
- `.gitleaks.toml`
- `scripts/changelog.ps1`, `scripts/tfm-parity.ps1`, `scripts/build-pr.ps1`,
  `scripts/third-party-notices.ps1`
- `.github/workflows/*.yml` and `.github/workflows/*.yaml`
- `.github/license-audit/*.json`

### 3. Credential protection

**Mechanism**: `persist-credentials: false`

Every checkout step sets `persist-credentials: false` so the checkout token is not written to git
config. Checkouts take the default ref of the triggering event (the PR merge ref on `pull_request`):

```yaml
- name: Checkout code
  uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1
  with:
    persist-credentials: false
```

**Note**: This keeps the token out of git config, but does NOT prevent a step from reading
`GITHUB_TOKEN` if it is explicitly exposed to that step.

### 4. Minimal permissions

Workflows default to read-only permissions:

```yaml
permissions:
  contents: read
```

In `pr.yaml` the only write scope is `security-events: write` on the `inspectcode-upload` job, which
never checks out PR code.

## Attack Scenarios Prevented

### Scenario 1: Malicious workflow modification
**Attack**: A PR edits `.github/workflows/pr.yaml` to disable a check, alongside code that would fail it
**Prevention**: The workflow file is protected; `protected-files.yaml` (running from `main`) fails a PR
that mixes it with other changes, and a workflow-only PR is reviewed on its own
**Status**: ✅ Protected

### Scenario 2: Configuration file tampering
**Attack**: A PR edits `.editorconfig` to disable analyzers, alongside code that violates them
**Prevention**: Same guard: mixed PRs fail `protected-files.yaml`
**Status**: ✅ Protected

### Scenario 3: Credential theft
**Attack**: PR code tries to read GitHub credentials during the build
**Prevention**: `pull_request` read-only token, no secrets, `persist-credentials: false`, minimal
permissions; the one `pull_request_target` workflow never runs PR code
**Status**: ✅ Protected

### Scenario 4: Code analysis bypass
**Attack**: A PR edits `BannedSymbols.txt` or a `.ruleset` to allow dangerous APIs and uses them
**Prevention**: Same guard: mixed PRs fail `protected-files.yaml`
**Status**: ✅ Protected

## Validation

Manual checks when reviewing changes to this model:

1. **Mixed PR**: open a PR that changes `.editorconfig` and a `.cs` file; the `Protected Files Guard`
   check must fail and list `.editorconfig` as protected.
2. **Configuration-only PR**: open a PR that changes only `.editorconfig`; the guard must pass with the
   "CONFIGURATION-ONLY PULL REQUEST" notice, and `pr.yaml` must build with the PR's copy of the file.

## Maintenance

### Making changes to protected configuration files

1. Put the protected-file change in a pull request of its own (nothing else in it). `pr.yaml` validates
   it with the changed configuration, and the guard passes with a notice so a maintainer reviews the diff.
2. Merge it, then open the pull request that depends on the new configuration.

If you need to relax a rule AND add code that the old rule would reject, that is two pull requests: the
configuration change first, then the code.

### Adding a new protected file

1. Add the path to the `case` statement (or the `grep -qiE` pattern) in `protected-files.yaml`.
2. Update the list in this document.

## Workflow-file static analysis

CodeQL (`codeql.yaml`) scans this repo's C# source. It does not inspect the GitHub Actions YAML itself.
That is `.github/workflows/actions-audit.yaml` (`name: Actions Audit`), which runs on every pull request
and on pushes to `main`:

- **[actionlint](https://github.com/rhysd/actionlint)**: YAML/expression syntax errors, invalid `uses:`
  refs, shellcheck issues inside `run:` blocks. Any finding fails the job.
- **[zizmor](https://github.com/zizmorcore/zizmor)**: untrusted-input injection into `run:` blocks,
  missing or overly broad `permissions:`, secret leakage, unpinned actions. Every finding uploads as
  SARIF to the Security tab, and High-severity findings also fail the job. A deliberate pattern is
  accepted with an inline `# zizmor: ignore[rule-id]` on the flagged key.

Because `actions-audit.yaml` lives under `.github/workflows/`, it is itself a protected file.

## References

- [GitHub Actions Security Hardening](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)
- [Keeping your GitHub Actions secure](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions#using-third-party-actions)
- [Understanding pull_request_target](https://securitylab.github.com/research/github-actions-preventing-pwn-requests/)
