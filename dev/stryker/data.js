window.BENCHMARK_DATA = {
  "lastUpdate": 1794142048075,
  "repoUrl": "https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions",
  "entries": {
    "Mutation score": [
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "9edfabefcde0d2192fae37623033c5d4df1c12d8",
          "message": "chore: take the template's remaining .gitignore entries (#452)\n\n* chore: take the template's remaining .gitignore entries\n\nAdditive: the entries this repository did not already have are appended with\nthe template's own comments, and nothing it does have is touched. A wholesale\ncopy would drop repo-specific entries and could move a !negation relative to\nthe pattern it exempts.\n\n  _site/                     DocFX output at any depth, not just\n                             docfx_project/_site/\n  docfx_project/api/*.yml    metadata docfx generates per public type; the\n                             hand-written index.md and README.md stay tracked\n  *.template                 the sidecars scripts/upgrade.ps1 writes for files\n                             needing a manual merge - resolve and delete them,\n                             never commit them\n\nChecked before appending: no tracked file in this repository matches any of\nthe new patterns.\n\nCo-Authored-By: Claude Opus 5 <noreply@anthropic.com>\n\n* docs(changelog): take the template's fragment README (repo-template#630) (#451)\n\nTwo additions: a label added after the fragment check ran does not reach a re-run (the re-run replays the original payload - push a commit instead), and the list of src/ files that never need a fragment (nested .editorconfig, globalconfig/ruleset/DotSettings, PublicAPI baselines) - the rule this repository's changelog.ps1 already implements.\n\nCo-authored-by: Claude Fable 5.1 <noreply@anthropic.com>\n\n---------\n\nCo-authored-by: Claude Opus 5 <noreply@anthropic.com>",
          "timestamp": "2026-09-22T22:56:25Z",
          "url": "https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/commit/9edfabefcde0d2192fae37623033c5d4df1c12d8"
        },
        "date": 1790489937025,
        "tool": "customBiggerIsBetter",
        "benches": [
          {
            "name": "Mutation score",
            "value": 100,
            "unit": "%"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "154123b4431bbeed9f0249ca053640842c2a4d54",
          "message": "fix: align Legacy namespaces with InspectCode CheckNamespace (#461)\n\n- Legacy csproj: RootNamespace = Wolfgang.Extensions.IAsyncEnumerable, the namespace the\n  package deliberately shares with the main package (clears code-scanning alert #464).\n- Legacy.Example: namespace LegacyExample -> Legacy.Example to match its folder (#463).\n\nCo-authored-by: Claude Opus 5.5 <noreply@anthropic.com>",
          "timestamp": "2026-09-28T18:22:29Z",
          "url": "https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/commit/154123b4431bbeed9f0249ca053640842c2a4d54"
        },
        "date": 1791101412590,
        "tool": "customBiggerIsBetter",
        "benches": [
          {
            "name": "Mutation score",
            "value": 100,
            "unit": "%"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "019d297821482455e7f996a77166a10b5f8399ac",
          "message": "ci: sync Stage 1 coverage gate and build-pr.ps1 local tools from repo-template (#469)\n\n* ci: sync Stage 1 coverage gate and build-pr.ps1 local tools from repo-template\n\nStage 1 now runs every test project (integration included), records the\nassemblies that ran, fails \"Check for coverage files\" when tests ran but\nproduced no coverage, and fails the gate for an assembly that ran without a\ncoverage row (Chris-Wolfgang/repo-template#654).\n\nbuild-pr.ps1 runs ReportGenerator and DevSkim as pinned local tools through a\nRestore-LocalTools helper, and a DevSkim non-zero exit fails the step\n(Chris-Wolfgang/repo-template#656).\n\nCo-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>\n\n* ci(pr): Stage 2 skips *.Tests.Integration.* after TFM discovery (repo-template#701)\n\nWindows runners cannot run the Linux containers integration suites need;\nStage 1 runs and gates them. A TFM no Linux stage runs is still reported.\n\nCo-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>\n\n---------\n\nCo-authored-by: Claude Opus 5.5 <noreply@anthropic.com>",
          "timestamp": "2026-10-11T02:18:35Z",
          "url": "https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/commit/019d297821482455e7f996a77166a10b5f8399ac"
        },
        "date": 1791699529533,
        "tool": "customBiggerIsBetter",
        "benches": [
          {
            "name": "Mutation score",
            "value": 100,
            "unit": "%"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "68a91ba15a0c530cf9b0e6dd48dab4a34cadb81a",
          "message": "feat: add SlidingWindowAsync (overlapping fixed-size windows) (#500)\n\nYields a window of `windowSize` consecutive elements per source element once the\nfirst window is full; a shorter source yields nothing. A circular buffer holds the\ncurrent window and each yielded window is a fresh oldest-first array, so a kept\nwindow never changes.\n\nReturns IReadOnlyList<T>, unlike ChunkAsync's ICollection<T>: positional access is\nthe point of a window. ADR-0006 (Proposed) records the asymmetry.\n\nShips with unit tests, a runnable example, a README row and a changelog fragment.\n\nCloses #123\n\nCo-authored-by: Claude Opus 5.5 <noreply@anthropic.com>",
          "timestamp": "2026-10-18T02:00:42Z",
          "url": "https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/commit/68a91ba15a0c530cf9b0e6dd48dab4a34cadb81a"
        },
        "date": 1792304251421,
        "tool": "customBiggerIsBetter",
        "benches": [
          {
            "name": "Mutation score",
            "value": 99.11,
            "unit": "%"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "dependabot[bot]",
            "username": "dependabot[bot]",
            "email": "49699333+dependabot[bot]@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "3006b765fbcbc504ce9ac7ec1206cbef3f10d4d2",
          "message": "build(deps): Bump the github-actions group with 4 updates (#562)\n\nBumps the github-actions group with 4 updates: [github/codeql-action/upload-sarif](https://github.com/github/codeql-action), [actions/setup-dotnet](https://github.com/actions/setup-dotnet), [github/codeql-action/init](https://github.com/github/codeql-action) and [github/codeql-action/analyze](https://github.com/github/codeql-action).\n\n\nUpdates `github/codeql-action/upload-sarif` from 4.39.1 to 4.39.2\n- [Release notes](https://github.com/github/codeql-action/releases)\n- [Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md)\n- [Commits](https://github.com/github/codeql-action/compare/9b3254085494b55a0f7c4c2b3d2d52cc712ce370...1fc58d04a4d4374f921bdac573c3811c240f9c26)\n\nUpdates `actions/setup-dotnet` from 6.0.0 to 6.1.0\n- [Release notes](https://github.com/actions/setup-dotnet/releases)\n- [Commits](https://github.com/actions/setup-dotnet/compare/a98b56852c35b8e3190ac28c8c2271da59106c68...0f4a354dd5547a28c70b7853dbd95d5b67886e73)\n\nUpdates `github/codeql-action/init` from 4.39.1 to 4.39.2\n- [Release notes](https://github.com/github/codeql-action/releases)\n- [Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md)\n- [Commits](https://github.com/github/codeql-action/compare/9b3254085494b55a0f7c4c2b3d2d52cc712ce370...1fc58d04a4d4374f921bdac573c3811c240f9c26)\n\nUpdates `github/codeql-action/analyze` from 4.39.1 to 4.39.2\n- [Release notes](https://github.com/github/codeql-action/releases)\n- [Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md)\n- [Commits](https://github.com/github/codeql-action/compare/9b3254085494b55a0f7c4c2b3d2d52cc712ce370...1fc58d04a4d4374f921bdac573c3811c240f9c26)\n\n---\nupdated-dependencies:\n- dependency-name: github/codeql-action/upload-sarif\n  dependency-version: 4.39.2\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: github-actions\n- dependency-name: actions/setup-dotnet\n  dependency-version: 6.1.0\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: github-actions\n- dependency-name: github/codeql-action/init\n  dependency-version: 4.39.2\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: github-actions\n- dependency-name: github/codeql-action/analyze\n  dependency-version: 4.39.2\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: github-actions\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Chris Wolfgang <210299580+Chris-Wolfgang@users.noreply.github.com>",
          "timestamp": "2026-11-08T02:25:29Z",
          "url": "https://github.com/Chris-Wolfgang/IAsyncEnumerable-Extensions/commit/3006b765fbcbc504ce9ac7ec1206cbef3f10d4d2"
        },
        "date": 1794142043397,
        "tool": "customBiggerIsBetter",
        "benches": [
          {
            "name": "Mutation score",
            "value": 100,
            "unit": "%"
          },
          {
            "name": "Mutation score (tests/Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit)",
            "value": 100,
            "unit": "%"
          }
        ]
      }
    ]
  }
}