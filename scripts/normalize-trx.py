#!/usr/bin/env python3
"""Normalize a .trx test-results file into a deterministic, platform-noise
-free text representation for cross-platform diffing (#235).

pr.yaml's Stage 1/2/3 already verify the suite PASSES on Linux/Windows/
macOS. This is a different question: does it pass the SAME WAY on every
platform? A test that's silently skipped, or a Theory that resolves a
different case count, or an assertion message that differs in wording
across platforms, all pass "green" on every OS individually while telling
this comparison something is wrong.

Usage:
    python3 normalize-trx.py <input.trx> <output.txt>

Strips everything platform/run-specific (timestamps, durations, machine
name, computer name, run IDs) and keeps only: test name, outcome, and
(for failures) the error message — sorted so file order doesn't matter.
"""

from __future__ import annotations

import sys
import xml.etree.ElementTree as ET

TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def main() -> int:
    if len(sys.argv) != 3:
        print(f"Usage: {sys.argv[0]} <input.trx> <output.txt>", file=sys.stderr)
        return 2

    input_path, output_path = sys.argv[1], sys.argv[2]
    tree = ET.parse(input_path)
    root = tree.getroot()

    lines = []
    for result in root.iter(f"{TRX_NS}UnitTestResult"):
        test_name = result.get("testName", "<unknown>")
        outcome = result.get("outcome", "<unknown>")

        message = ""
        if outcome != "Passed":
            error_info = result.find(f"{TRX_NS}Output/{TRX_NS}ErrorInfo/{TRX_NS}Message")
            if error_info is not None and error_info.text:
                message = error_info.text.strip().splitlines()[0]

        lines.append(f"{outcome}\t{test_name}\t{message}")

    lines.sort()

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + ("\n" if lines else ""))

    print(f"Wrote {len(lines)} normalized result(s) to {output_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
