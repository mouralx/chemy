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

    report_path = matches[0]
    print(f"Parsing coverage report: {report_path}")

    tree = ET.parse(report_path)
    root = tree.getroot()

    line_rate = float(root.attrib.get("line-rate", 0.0)) * 100.0
    branch_rate = float(root.attrib.get("branch-rate", 0.0)) * 100.0
    lines_covered = int(root.attrib.get("lines-covered", 0))
    lines_valid = int(root.attrib.get("lines-valid", 0))
    branches_covered = int(root.attrib.get("branches-covered", 0))
    branches_valid = int(root.attrib.get("branches-valid", 0))

    print("\n================ COVERAGE VERIFICATION REPORT ================")
    print(f"Line Coverage:   {line_rate:6.2f}% ({lines_covered} / {lines_valid} lines) [Threshold >= {MIN_LINE_COVERAGE:.1f}%]")
    print(f"Branch Coverage: {branch_rate:6.2f}% ({branches_covered} / {branches_valid} branches) [Threshold >= {MIN_BRANCH_COVERAGE:.1f}%]")
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
