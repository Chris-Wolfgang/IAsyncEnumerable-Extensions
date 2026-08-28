# Copilot Coding Agent Instructions

## Repository Summary

This is a .NET library project that provides extension methods for `IAsyncEnumerable<T>`. The library is designed to make working with async sequences easier and more efficient, with features like chunking, batching, and other utility methods.

**Repository Type**: .NET Library Project  
**Target Frameworks**: .NET Framework 4.6.2, .NET Standard 2.0, .NET 8.0, .NET 10.0 (main package); .NET Framework 4.6.2, .NET Standard 2.0 only (Legacy package)  
**Primary Language**: C#  
**Packages**: Wolfgang.Extensions.IAsyncEnumerable (main) and Wolfgang.Extensions.IAsyncEnumerable.Legacy (terminal operators for TFMs without System.Linq.AsyncEnumerable), both under `src/`  

## Build and Validation Instructions

### Prerequisites
- .NET 10.0.x SDK (always install if not present)
- ReportGenerator tool (installed via `dotnet tool install -g dotnet-reportgenerator-globaltool`)
- DevSkim CLI (installed via `dotnet tool install --global Microsoft.CST.DevSkim.CLI`)

### Build Process

1. **Restore Dependencies** (always run first):
   ```powershell
   dotnet restore
   ```

2. **Build Solution**:
   ```powershell
   dotnet build --no-restore --configuration Release
   ```

3. **Run Tests with Coverage**:
   ```powershell
   # Find and test all test projects
   Get-ChildItem -Path ./tests -Filter '*Test*.csproj' -Recurse | ForEach-Object {
     dotnet test $_.FullName --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory "./TestResults"
   }
   ```

4. **Generate Coverage Reports**:
   ```powershell
   reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html;TextSummary;MarkdownSummaryGithub;CsvSummary"
   ```

5. **Security Scanning**:
   ```powershell
   devskim analyze --source-code . -f text --output-file devskim-results.txt -E
   ```

### Critical Build Requirements
- **Code Coverage**: Minimum 90% line coverage required for all projects
- **Security Scanning**: DevSkim must pass with no errors
- **Build Configuration**: Always use Release configuration for CI
- **Test Pattern**: Test projects must match `*Test*.csproj` pattern in `/tests` folder

### Common Issues and Workarounds
- **Timeout Issues**: Coverage and security scans can take 5-10 minutes for larger projects
- **Coverage Threshold Failures**: If below 90%, the build will fail - this is by design
- **Missing Test Projects**: The workflow expects at least one test project in `/tests` folder
- **DevSkim False Positives**: Review `devskim-results.txt` for any security findings

## Project Layout and Architecture

### Directory Structure
```
root/
├── IAsyncEnumerable Extensions.slnx    # Solution file
├── src/                                # Library projects
│   ├── Wolfgang.Extensions.IAsyncEnumerable/
│   │   ├── Wolfgang.Extensions.IAsyncEnumerable.csproj
│   │   └── IAsyncEnumerableExtensions.cs
│   └── Wolfgang.Extensions.IAsyncEnumerable.Legacy/
│       ├── Wolfgang.Extensions.IAsyncEnumerable.Legacy.csproj
│       └── IAsyncEnumerableLegacyExtensions.cs
├── tests/                              # Test projects
│   └── Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit/
├── benchmarks/                         # Performance benchmarks
│   └── Wolfgang.Extensions.IAsyncEnumerable.Benchmarks/
├── examples/                           # Example usage projects
├── docs/                               # Documentation
└── .github/                            # GitHub configuration
```

### Key Configuration Files
- **`.editorconfig`**: Code style rules (C# file-scoped namespaces, var preferences, analyzer severity)
- **`.gitignore`**: Comprehensive .NET gitignore (Visual Studio, build artifacts, packages)
- **`CONTRIBUTING.md`**: Contribution guidelines
- **`CODE_OF_CONDUCT.md`**: Standard Contributor Covenant v2.0

### GitHub Integration
- **Workflows**: `.github/workflows/pr.yaml` - Comprehensive CI/CD pipeline
- **Issue Templates**: Bug reports, feature requests, and maintenance tasks (all YAML)
- **PR Template**: Structured pull request template with checklists
- **CODEOWNERS**: Default owner `@Chris-Wolfgang`, update usernames as needed
- **Dependabot**: Configured for NuGet packages in all project directories

### Continuous Integration Pipeline (`.github/workflows/pr.yaml`)
The workflow runs on pull requests to `main` branch and includes:

1. **Environment**: Multi-stage pipeline (Linux, Windows, macOS) with .NET SDKs 3.1.x through 10.0.x
2. **Build Steps**: Checkout → Setup .NET → Restore → Build → Test → Coverage → Security
3. **Artifacts**: Coverage reports and DevSkim results uploaded
4. **Branch Protection**: Configured to require this workflow to pass before merging

### Branch Protection Configuration
Branch protection rulesets are managed in GitHub (Settings → Rules → Rulesets). The local PowerShell script `scripts/Fix-BranchRuleset.ps1` updates the ruleset's required status checks; `scripts/Setup-Labels.ps1` provisions the repo's label set.

**Single-Developer Configuration (Default):**
- No PR approvals required (you can merge your own PRs)
- Allows solo developers to merge their own PRs while still enforcing CI/CD checks

**Multi-Developer Configuration:**
- Requires 1+ approval before merging
- Requires code owner review

**All Configurations Include:**
- Require status checks to pass before merging
- Require branches to be up to date
- Require conversation resolution before merging
- Restrict deletions and block force pushes
- Require code scanning (CodeQL High+ severity)

**Branch Protection Setup Instructions:**
1. Install GitHub CLI (gh) from https://cli.github.com/
2. Authenticate: `gh auth login`
3. Create/adjust the ruleset in GitHub (Settings → Rules → Rulesets), then keep its required status checks in sync with `pr.yaml` by running:
   ```powershell
   pwsh -File ./scripts/Fix-BranchRuleset.ps1
   ```

## Key Files and Locations

### Root Directory Files
- `README.md` - Project description and documentation
- `LICENSE` - MIT License
- `.editorconfig` - Code style configuration
- `.gitignore` - .NET-specific gitignore

### GitHub Directory (`.github/`)
- `workflows/pr.yaml` - Main CI/CD pipeline
- `ISSUE_TEMPLATE/` - Bug report (YAML) and feature request templates
- `pull_request_template.md` - PR template with checklists
- `CODEOWNERS` - Code ownership rules
- `dependabot.yml` - Dependency update configuration

### Project Directories
- `src/Wolfgang.Extensions.IAsyncEnumerable/` - Main library source code
- `tests/` - Unit and integration tests
- `benchmarks/` - Performance benchmarks
- `examples/` - Example usage projects
- `docs/` - Documentation and API reference

## Agent Guidelines

### Trust These Instructions
This information has been validated against the project structure and GitHub workflows. **Only search for additional information if these instructions are incomplete or found to be incorrect.**

### When Working with This Project
1. **Main Library**: The core library is in `src/Wolfgang.Extensions.IAsyncEnumerable/`
2. **Adding Dependencies**: Use `dotnet add package` commands
3. **Code Style**: Follow `.editorconfig` rules (file-scoped namespaces, explicit typing)
4. **Testing**: Ensure test projects follow `*Test*.csproj` naming convention
5. **Coverage**: Aim for >90% code coverage to pass CI
6. **Security**: Review DevSkim findings and address security concerns
7. **Multi-targeting**: The library targets .NET Framework 4.6.2, .NET Standard 2.0, .NET 8.0, and .NET 10.0

### Validation Steps
Before submitting changes:
1. Run `dotnet restore && dotnet build --configuration Release`
2. Run tests with coverage collection
3. Verify coverage meets 90% threshold
4. Run DevSkim security scan
5. Ensure all GitHub Actions checks pass

This project provides extension methods for IAsyncEnumerable with enterprise-grade CI/CD, security scanning, and development best practices built-in.