#!/usr/bin/env python3
"""Compare a PR-head BenchmarkDotNet run against a PR-base run and render
a markdown delta table for a PR comment (#249).

Usage:
    python3 compare-pr-benchmarks.py <head-report.json> <base-report.json> <output.md>

Exit codes: 0 = every benchmark's allocations are within tolerance,
3 = at least one benchmark's allocation regressed beyond ALLOC_TOLERANCE,
anything else (e.g. 2 for usage errors, or an uncaught exception) = crash.
Only allocation regressions gate; time regressions beyond TIME_TOLERANCE
are advisory-only — flagged in the table but never affect the exit code
(shared-runner wall-clock is too noisy to gate on). The caller
(pr-benchmarks.yaml) is responsible for treating exit 3 as advisory-only
when the PR carries the `perf-impact-acknowledged` label — this script
only computes and reports, it doesn't know about labels.
"""

from __future__ import annotations

import json
import sys

TIME_TOLERANCE = 0.20  # 20% slower
ALLOC_TOLERANCE = 0.50  # 50% more allocated


def load(path: str) -> dict[str, dict[str, float]]:
    with open(path, encoding="utf-8") as f:
        data = json.load(f)

    results = {}
    for benchmark in data.get("Benchmarks", []):
        name = benchmark.get("FullName") or benchmark.get("Method") or "?"
        stats = benchmark.get("Statistics", {}) or {}
        memory = benchmark.get("Memory", {}) or {}
        results[name] = {
            "meanNanoseconds": stats.get("Mean", 0.0),
            "allocatedBytes": memory.get("BytesAllocatedPerOperation", 0),
        }
    return results


def format_ns(ns: float) -> str:
    if ns >= 1_000_000:
        return f"{ns / 1_000_000:.2f} ms"
    if ns >= 1_000:
        return f"{ns / 1_000:.2f} us"
    return f"{ns:.1f} ns"


def main() -> int:
    if len(sys.argv) != 4:
        print(f"Usage: {sys.argv[0]} <head-report.json> <base-report.json> <output.md>", file=sys.stderr)
        return 2

    head_path, base_path, output_path = sys.argv[1], sys.argv[2], sys.argv[3]
    head = load(head_path)
    base = load(base_path)

    rows = []
    any_regression = False

    for name in sorted(set(head) | set(base)):
        h = head.get(name)
        b = base.get(name)

        if h is None:
            # Benchmark exists only in the base run. The table's alloc column
            # is head-side, so show the base time in the base column and em
            # dashes for the head-side cells rather than misfiling base data.
            rows.append(f"| `{name}` | _removed_ | {format_ns(b['meanNanoseconds'])} | — | — | — |")
            continue
        if b is None:
            rows.append(f"| `{name}` | {format_ns(h['meanNanoseconds'])} | _new_ | — | {h['allocatedBytes']:.0f} B | — |")
            continue

        time_delta = (h["meanNanoseconds"] - b["meanNanoseconds"]) / b["meanNanoseconds"] if b["meanNanoseconds"] else 0.0
        alloc_delta = (h["allocatedBytes"] - b["allocatedBytes"]) / b["allocatedBytes"] if b["allocatedBytes"] else 0.0

        time_flag = " ⚠️" if time_delta > TIME_TOLERANCE else ""
        alloc_flag = " 🔴" if alloc_delta > ALLOC_TOLERANCE else ""

        if alloc_delta > ALLOC_TOLERANCE:
            any_regression = True

        rows.append(
            f"| `{name}` | {format_ns(h['meanNanoseconds'])} | {format_ns(b['meanNanoseconds'])} | "
            f"{time_delta:+.1%}{time_flag} | {h['allocatedBytes']:.0f} B | {alloc_delta:+.1%}{alloc_flag} |"
        )

    lines = [
        "<!-- pr-benchmarks-delta -->",
        "## Benchmark delta vs base branch",
        "",
        "| Benchmark | Head time | Base time | Time Δ | Head alloc | Alloc Δ |",
        "|---|---:|---:|---:|---:|---:|",
        *rows,
        "",
        f"⚠️ = time regressed > {TIME_TOLERANCE:.0%} (**advisory only** — shared-runner wall-clock is noisy, doesn't fail the check).",
        f"🔴 = allocation regressed > {ALLOC_TOLERANCE:.0%} (**fails the check** unless this PR carries the `perf-impact-acknowledged` label).",
    ]

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")

    # Print status only (not the table itself) to stdout — some local
    # Windows consoles are cp1252 and choke on the U+0394/emoji glyphs
    # above; the actual markdown file is UTF-8 and that's what the
    # workflow posts as the PR comment.
    print(f"Wrote {output_path}: {'REGRESSION' if any_regression else 'within tolerance'}")

    # 3 (not 1) so the caller can tell "allocation regression beyond
    # tolerance" apart from a crash: 0 = clean, 3 = regression, other = crash.
    return 3 if any_regression else 0


if __name__ == "__main__":
    sys.exit(main())
