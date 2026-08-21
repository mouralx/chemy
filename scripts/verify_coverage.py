#!/usr/bin/env python3
"""
Chemy Code Coverage Verification Gate
====================================
Parses Cobertura coverage reports and enforces strict Line Coverage (>= 80.0%)
and Branch Coverage (>= 70.0%) floors across the repository solution.
"""

import glob
import os
import sys
import xml.etree.ElementTree as ET

MIN_LINE_COVERAGE = 80.0
MIN_BRANCH_COVERAGE = 70.0

def verify_coverage(report_pattern: str):
    matches = glob.glob(report_pattern, recursive=True)
    if not matches:
        print(f"ERROR: No coverage reports matching '{report_pattern}' were found.", file=sys.stderr)
        sys.exit(1)

    total_lines_covered = 0
    total_lines_valid = 0
    total_branches_covered = 0
    total_branches_valid = 0

    print(f"Found {len(matches)} coverage report(s):")
    for r in matches:
        print(f"  - {r}")
        tree = ET.parse(r)
        root = tree.getroot()
        total_lines_covered += int(root.attrib.get("lines-covered", 0))
        total_lines_valid += int(root.attrib.get("lines-valid", 0))
        total_branches_covered += int(root.attrib.get("branches-covered", 0))
        total_branches_valid += int(root.attrib.get("branches-valid", 0))

    if total_lines_valid == 0:
        print("ERROR: Total valid lines is 0 in coverage report(s).", file=sys.stderr)
        sys.exit(1)

    line_rate = (total_lines_covered / total_lines_valid) * 100.0
    branch_rate = (total_branches_covered / total_branches_valid) * 100.0 if total_branches_valid > 0 else 0.0

    print("\n================ COVERAGE VERIFICATION REPORT ================")
    print(f"Line Coverage:   {line_rate:6.2f}% ({total_lines_covered} / {total_lines_valid} lines) [Threshold >= {MIN_LINE_COVERAGE:.1f}%]")
    print(f"Branch Coverage: {branch_rate:6.2f}% ({total_branches_covered} / {total_branches_valid} branches) [Threshold >= {MIN_BRANCH_COVERAGE:.1f}%]")
    print("=============================================================\n")

    failed = False
    if line_rate < MIN_LINE_COVERAGE:
        print(f"::error::Line coverage {line_rate:.2f}% is below required threshold of {MIN_LINE_COVERAGE:.1f}%", file=sys.stderr)
        failed = True

    if branch_rate < MIN_BRANCH_COVERAGE:
        print(f"::error::Branch coverage {branch_rate:.2f}% is below required threshold of {MIN_BRANCH_COVERAGE:.1f}%", file=sys.stderr)
        failed = True

    if failed:
        sys.exit(1)

    print("SUCCESS: Code coverage meets all required scientific quality floors.")

if __name__ == "__main__":
    pattern = sys.argv[1] if len(sys.argv) > 1 else "**/coverage.cobertura.xml"
    verify_coverage(pattern)
