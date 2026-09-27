window.BENCHMARK_DATA = {
  "lastUpdate": 1790489942860,
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
      }
    ]
  }
}