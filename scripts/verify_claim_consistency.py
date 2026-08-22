#!/usr/bin/env python3
"""Fail CI when active documentation drifts from executable scientific contracts."""

from __future__ import annotations

import hashlib
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]


def read(relative_path: str) -> str:
    return (ROOT / relative_path).read_text(encoding="utf-8")


def sha256(relative_path: str) -> str:
    return hashlib.sha256((ROOT / relative_path).read_bytes()).hexdigest()


def main() -> int:
    errors: list[str] = []

    active_docs = {
        "README.md": read("README.md"),
        "docs/ARCHITECTURE.md": read("docs/ARCHITECTURE.md"),
        "docs/API_REFERENCE.md": read("docs/API_REFERENCE.md"),
        "docs/SCIENTIFIC_APPROACH.md": read("docs/SCIENTIFIC_APPROACH.md"),
        "docs/SCIENTIFIC_CREDIBILITY_REPORT.md": read("docs/SCIENTIFIC_CREDIBILITY_REPORT.md"),
        "docs/SCIENTIFIC_VERIFICATION_BENCHMARKS.md": read("docs/SCIENTIFIC_VERIFICATION_BENCHMARKS.md"),
        "src/Chemy.Web/Pages/Index.cshtml": read("src/Chemy.Web/Pages/Index.cshtml"),
    }

    forbidden_claims = {
        "4-term": "The force-field implementation and public contract contain five terms.",
        "4-Term": "The force-field implementation and public contract contain five terms.",
        "Zero Allocations": "The optimizer allocates bounded topology and working arrays.",
        "Held-Out Verified": "The current UFF expansion is a post-development regression partition.",
        "Untouched During Implementation": "The repository does not demonstrate prospective holdout chronology.",
    }
    for path, content in active_docs.items():
        for forbidden, reason in forbidden_claims.items():
            if forbidden in content:
                errors.append(f"{path}: forbidden claim '{forbidden}'. {reason}")

    current_contract_files = {
        "scripts/generate_uff_reference.py": read("scripts/generate_uff_reference.py"),
        "src/Chemy.Core.Tests/ValidationData/ScientificBenchmarkValidationTests.cs": read(
            "src/Chemy.Core.Tests/ValidationData/ScientificBenchmarkValidationTests.cs"
        ),
        "src/Chemy.Core.Tests/ValidationData/rdkit_uff_butane_reference.json": read(
            "src/Chemy.Core.Tests/ValidationData/rdkit_uff_butane_reference.json"
        ),
    }
    for path, content in current_contract_files.items():
        if "held_out_molecules" in content:
            errors.append(f"{path}: legacy held_out_molecules contract remains")
        if "expanded_regression_molecules" not in content:
            errors.append(f"{path}: expanded_regression_molecules contract is missing")

    hash_contracts = [
        (
            "src/Chemy.Core.Tests/ValidationData/rdkit_uff_butane_reference.json",
            ["src/Chemy.Core.Tests/ValidationData/ScientificBenchmarkValidationTests.cs", "docs/SCIENTIFIC_VERIFICATION_BENCHMARKS.md"],
        ),
        (
            "src/Chemy.Core.Tests/ValidationData/experimental_nmr_reference.json",
            ["src/Chemy.Core.Tests/ValidationData/ScientificBenchmarkValidationTests.cs"],
        ),
        (
            "src/Chemy.Core.Tests/ValidationData/crc_iupac_reduction_potentials.json",
            ["src/Chemy.Core.Tests/ValidationData/ScientificBenchmarkValidationTests.cs", "docs/SCIENTIFIC_VERIFICATION_BENCHMARKS.md"],
        ),
    ]
    for artifact, consumers in hash_contracts:
        digest = sha256(artifact)
        for consumer in consumers:
            if digest not in read(consumer):
                errors.append(f"{consumer}: does not pin current SHA-256 {digest} for {artifact}")

    shomate_source = read("src/Chemy.Core/Thermodynamics/ShomateThermodynamics.cs")
    required_shomate_contracts = [
        "new(-6.387880, 184.4019, -112.9718, 28.49593",
        "new(106.5104, 13.73260, -2.628481, 0.174595",
        "No extrapolation outside published intervals",
        "ShomateTemperatureRange",
    ]
    for required in required_shomate_contracts:
        if required not in shomate_source:
            errors.append(f"Shomate source contract is missing: {required}")

    force_field_source = read("src/Chemy.Core/Physics/ForceFieldEngine.cs")
    for required in ["ForceFieldEnergyComponents", "isCarbonylCarbon ? 50.0 : 6.0"]:
        if required not in force_field_source:
            errors.append(f"Force-field source contract is missing: {required}")

    api_source = read("src/Chemy.Api/Program.cs")
    api_settings = read("src/Chemy.Api/appsettings.json")
    required_enterprise_contracts = {
        "API source": (
            api_source,
            [
                "AddProblemDetails",
                "AddRateLimiter",
                "FixedTimeEquals",
                "X-Correlation-ID",
                "MaxRequestBodySize",
                "ExposeDocumentation",
            ],
        ),
        "API settings": (
            api_settings,
            [
                '"RequireApiKey": true',
                '"ApiKey": ""',
                '"AllowedOrigins": []',
                '"AllowedHosts": "localhost;127.0.0.1"',
            ],
        ),
    }
    for label, (content, requirements) in required_enterprise_contracts.items():
        for required in requirements:
            if required not in content:
                errors.append(f"{label} contract is missing: {required}")

    if errors:
        print("Scientific claim consistency FAILED:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print("PASS: active scientific claims, artifact hashes, and executable contracts are consistent")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
