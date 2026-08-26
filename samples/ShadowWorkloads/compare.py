#!/usr/bin/env python3
"""Compare a fresh shadow-workload BDN run against the committed baseline.

Gate = allocation (hard, fails on > ALLOCATION_TOLERANCE regression).
Latency is reported but advisory-only — shared GitHub-hosted runner
wall-clock is too noisy to fail on (see reference_thorough_review_impl_patterns:
the same reasoning dropped the #147 24h soak and made #152/#140's gate
allocation-only across the fleet).

Usage:
    python3 compare.py <results-directory> <baseline.json>

<results-directory> is BDN's `BenchmarkDotNet.Artifacts/results/` — the
`--exporters json` full exporter writes one `*-report-full-compressed.json`
per benchmark CLASS (not one combined file across the whole run), so this
script globs and merges all of them.

Exits 0 if every scenario's allocated-bytes-per-op is within tolerance of
its baseline, 1 otherwise (with a diagnostic table on stdout either way).
"""

from __future__ import annotations

import glob
import json
import os
import sys

ALLOCATION_TOLERANCE = 0.50  # 50% — matches the fleet-wide shadow/GC gates
LATENCY_TOLERANCE = 0.20  # 20% — advisory only, never fails the run


def load_bdn_reports(results_directory: str) -> dict[str, dict[str, float]]:
    results: dict[str, dict[str, float]] = {}
    report_paths = sorted(glob.glob(os.path.join(results_directory, "*-report-full-compressed.json")))

    for path in report_paths:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)

        for benchmark in data.get("Benchmarks", []):
            name = benchmark["Method"]
            stats = benchmark.get("Statistics", {}) or {}
            memory = benchmark.get("Memory", {}) or {}
            results[name] = {
                "meanNanoseconds": stats.get("Mean", 0.0),
                "allocatedBytes": memory.get("BytesAllocatedPerOperation", 0),
            }

    return results


def load_baseline(path: str) -> dict[str, dict[str, float]]:
    with open(path, encoding="utf-8") as f:
        data = json.load(f)
    return data.get("scenarios", {})


def main() -> int:
    if len(sys.argv) != 3:
        print(f"Usage: {sys.argv[0]} <results-directory> <baseline.json>", file=sys.stderr)
        return 2

    results_directory, baseline_path = sys.argv[1], sys.argv[2]
    fresh = load_bdn_reports(results_directory)
    baseline = load_baseline(baseline_path)

    if not fresh:
        print(f"::error::No benchmarks found under {results_directory} — the run likely failed silently.")
        return 1

    failed = []
    print(f"{'Scenario':<55} {'Alloc (B)':>12} {'Baseline':>12} {'Delta':>8}  {'Latency (ns)':>14} {'Baseline':>14} {'Delta':>8}")
    print("-" * 130)

    for name, current in sorted(fresh.items()):
        base = baseline.get(name)
        if base is None:
            print(f"{name:<55} (no baseline entry — treating as informational only, not gated)")
            continue

        alloc_current = current["allocatedBytes"]
        alloc_base = base["allocatedBytes"]
        alloc_delta = (alloc_current - alloc_base) / alloc_base if alloc_base else 0.0

        lat_current = current["meanNanoseconds"]
        lat_base = base["meanNanoseconds"]
        lat_delta = (lat_current - lat_base) / lat_base if lat_base else 0.0

        alloc_flag = "FAIL" if alloc_delta > ALLOCATION_TOLERANCE else "ok"
        lat_flag = "warn" if lat_delta > LATENCY_TOLERANCE else "ok"

        print(
            f"{name:<55} {alloc_current:>12.0f} {alloc_base:>12.0f} {alloc_delta:>7.1%}{'*' if alloc_flag == 'FAIL' else ' '} "
            f"{lat_current:>14.0f} {lat_base:>14.0f} {lat_delta:>7.1%}{'*' if lat_flag == 'warn' else ' '}"
        )

        if alloc_flag == "FAIL":
            failed.append(
                f"{name}: allocation regressed {alloc_delta:.1%} "
                f"({alloc_base:.0f} B -> {alloc_current:.0f} B), exceeds {ALLOCATION_TOLERANCE:.0%} tolerance"
            )
        if lat_flag == "warn":
            print(f"::warning::{name}: latency regressed {lat_delta:.1%} (advisory only, not gated)")

    if failed:
        print("\n::error::Allocation regression(s) exceeded tolerance:")
        for f in failed:
            print(f"  - {f}")
        return 1

    print("\nAll scenarios within allocation tolerance.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
