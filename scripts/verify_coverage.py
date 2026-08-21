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
import re

MIN_LINE_COVERAGE = 80.0
MIN_BRANCH_COVERAGE = 70.0

def verify_coverage(report_pattern: str):
    matches = sorted(glob.glob(report_pattern, recursive=True))
    if not matches:
        print(f"ERROR: No coverage reports matching '{report_pattern}' were found.", file=sys.stderr)
        sys.exit(1)

    valid_lines = set()
    covered_lines = set()
    branch_info = {}

    fallback_lines_covered = 0
    fallback_lines_valid = 0
    fallback_branches_covered = 0
    fallback_branches_valid = 0

    has_line_detail = False

    print(f"Found {len(matches)} coverage report(s):")
    for r in matches:
        print(f"  - {r}")
        tree = ET.parse(r)
        root = tree.getroot()
        
        fallback_lines_covered += int(root.attrib.get("lines-covered", 0))
        fallback_lines_valid += int(root.attrib.get("lines-valid", 0))
        fallback_branches_covered += int(root.attrib.get("branches-covered", 0))
        fallback_branches_valid += int(root.attrib.get("branches-valid", 0))

        # Iterate over all packages and classes to extract line details and deduplicate by (pkg_name, norm_filename, line_number)
        for pkg in root.findall(".//package"):
            pkg_name = pkg.attrib.get("name", "")
            for cls in pkg.findall(".//class"):
                filename = cls.attrib.get("filename", "")
                norm_filename = os.path.normpath(filename) if filename else cls.attrib.get("name", "")
                for line in cls.findall(".//line"):
                    has_line_detail = True
                    number = line.attrib.get("number")
                    if not number:
                        continue
                    
                    line_id = (pkg_name, norm_filename, number)
                    hits = int(line.attrib.get("hits", "0"))
                    valid_lines.add(line_id)
                    
                    if hits > 0:
                        covered_lines.add(line_id)
                        
                    is_branch = line.attrib.get("branch", "false").lower() == "true"
                    if is_branch:
                        cond_cov = line.attrib.get("condition-coverage", "")
                        m = re.search(r'\((\d+)/(\d+)\)', cond_cov)
                        if m:
                            cov = int(m.group(1))
                            val = int(m.group(2))
                            if line_id not in branch_info:
                                branch_info[line_id] = [cov, val]
                            else:
                                # Merge branch coverage by taking the max covered branches seen for this line
                                branch_info[line_id][0] = max(branch_info[line_id][0], cov)
                                branch_info[line_id][1] = max(branch_info[line_id][1], val)

    if has_line_detail:
        total_lines_valid = len(valid_lines)
        total_lines_covered = len(covered_lines)
        total_branches_valid = sum(val for cov, val in branch_info.values())
        total_branches_covered = sum(cov for cov, val in branch_info.values())
    else:
        # Limitation: Cobertura doesn't have line-level detail in these reports.
        print("WARNING: Cobertura reports lack line-level detail. Falling back to root counters.", file=sys.stderr)
        if len(matches) > 1:
            print("WARNING: Multiple reports found without line-level detail. Aggregation will double-count coverage!", file=sys.stderr)
        
        total_lines_covered = fallback_lines_covered
        total_lines_valid = fallback_lines_valid
        total_branches_covered = fallback_branches_covered
        total_branches_valid = fallback_branches_valid

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
