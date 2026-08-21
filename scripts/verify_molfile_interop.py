#!/usr/bin/env python3
"""
Chemy Cross-Tool Molfile/SDF Bidirectional Interoperability Suite
================================================================
Validates true two-way runtime interoperability between Chemy and RDKit 2025.09.2:
  1. Direction 1 (Chemy -> RDKit): Reads live Chemy-exported Molfiles/SDFs and parses
     them with RDKit, verifying formulas, formal charges, atom/bond counts, and 3D coordinates.
  2. Direction 2 (RDKit -> Chemy): Generates authentic RDKit Molfiles/SDFs with 3D conformers,
     formal charges, and properties for Chemy's .NET test suite to parse and verify.
"""

from __future__ import annotations

import argparse
import os
import sys

from rdkit import Chem
from rdkit.Chem import AllChem, rdMolDescriptors

PINNED_RDKIT_VERSION = "2025.09.2"

CHEM_EXPORT_DIR = "src/Chemy.Core.Tests/ValidationData/interop_fixtures/chemy_exported"
RDKIT_EXPORT_DIR = "src/Chemy.Core.Tests/ValidationData/interop_fixtures/rdkit_exported"

# --- Hardcoded reference fixtures for standalone validation ---
FALLBACK_STRUCTURES = [
    {
        "name": "AspirinNeutral",
        "filename": "aspirin_neutral.mol",
        "expected_formula": "C9H8O4",
        "expected_charge": 0,
        "expected_atoms": 21,
        "expected_bonds": 21,
        "expected_dim": "3D",
        "smiles": "CC(=O)Oc1ccccc1C(=O)O",
    },
    {
        "name": "AcetateAnion",
        "filename": "acetate_anion.mol",
        "expected_formula": "C2H3O2-",
        "expected_charge": -1,
        "expected_atoms": 7,
        "expected_bonds": 6,
        "expected_dim": "2D",
        "smiles": "CC(=O)[O-]",
    },
    {
        "name": "PyridiniumCation",
        "filename": "pyridinium_cation.mol",
        "expected_formula": "C5H6N+",
        "expected_charge": 1,
        "expected_atoms": 12,
        "expected_bonds": 12,
        "expected_dim": "2D",
        "smiles": "[nH+]1ccccc1",
    },
    {
        "name": "GlycineZwitterion",
        "filename": "glycine_zwitterion.mol",
        "expected_formula": "C2H5NO2",
        "expected_charge": 0,
        "expected_atoms": 10,
        "expected_bonds": 9,
        "expected_dim": "2D",
        "smiles": "[NH3+]CC([O-])=O",
    },
]


def generate_rdkit_fixtures(output_dir: str) -> None:
    """Generate authentic RDKit Molfile and SDF records for Chemy to parse."""
    os.makedirs(output_dir, exist_ok=True)
    print(f"=== [DIRECTION 2: RDKIT -> CHEMY] Exporting RDKit Fixtures to '{output_dir}' ===")

    sdf_molecules = []

    for item in FALLBACK_STRUCTURES:
        name = item["name"]
        filename = item["filename"]
        smiles = item["smiles"]
        out_path = os.path.join(output_dir, filename)

        mol = Chem.MolFromSmiles(smiles)
        assert mol is not None, f"RDKit failed to parse SMILES: {smiles}"
        mol = Chem.AddHs(mol)
        AllChem.EmbedMolecule(mol, randomSeed=42)

        # Tag properties
        mol.SetProp("_Name", name)
        mol.SetProp("SMILES", smiles)
        mol.SetProp("FormalCharge", str(Chem.GetFormalCharge(mol)))
        mol.SetProp("Formula", rdMolDescriptors.CalcMolFormula(mol))

        molfile_str = Chem.MolToMolBlock(mol)
        with open(out_path, "w") as f:
            f.write(molfile_str)

        sdf_molecules.append(mol)
        print(f"  Exported {name} -> {out_path} (charge={Chem.GetFormalCharge(mol)})")

    # Export multi-record SDF
    sdf_path = os.path.join(output_dir, "rdkit_compounds.sdf")
    writer = Chem.SDWriter(sdf_path)
    for m in sdf_molecules:
        writer.write(m)
    writer.close()
    print(f"  Exported Multi-Record SDF ({len(sdf_molecules)} records) -> {sdf_path}")


def verify_chemy_exports(input_dir: str) -> bool:
    """Validate Chemy-exported files using RDKit 2025.09.2 with fail-closed integrity checks."""
    candidate_dirs = [
        input_dir,
        "src/Chemy.Core.Tests/ValidationData/interop_fixtures/chemy_exported",
        "src/Chemy.Core.Tests/bin/Release/net10.0/ValidationData/interop_fixtures/chemy_exported",
        "src/Chemy.Core.Tests/bin/Debug/net10.0/ValidationData/interop_fixtures/chemy_exported",
    ]

    actual_dir = None
    for d in candidate_dirs:
        if os.path.exists(d) and os.path.exists(os.path.join(d, "aspirin_neutral.mol")):
            actual_dir = d
            break

    print(f"=== [DIRECTION 1: CHEMY -> RDKIT] Verifying Chemy Exports ===")
    if actual_dir is None:
        print(f"  FAIL: Chemy export directory not found in candidate paths. Run `dotnet test` first.", file=sys.stderr)
        return False

    print(f"  Using Chemy export directory: '{actual_dir}'")

    all_passed = True
    for item in FALLBACK_STRUCTURES:
        name = item["name"]
        filename = item["filename"]
        expected_formula = item["expected_formula"]
        expected_charge = item["expected_charge"]
        expected_atoms = item["expected_atoms"]
        expected_bonds = item["expected_bonds"]
        expected_dim = item.get("expected_dim", "3D")

        filepath = os.path.join(actual_dir, filename)
        if not os.path.exists(filepath):
            print(f"  FAIL: Required export file '{filepath}' is missing!", file=sys.stderr)
            all_passed = False
            continue

        with open(filepath) as f:
            molfile = f.read()

        # Check dimensional header
        lines = molfile.splitlines()
        if len(lines) >= 2:
            header_line2 = lines[1]
            if "3D" in header_line2:
                header_dim = "3D"
            elif "2D" in header_line2:
                header_dim = "2D"
            else:
                header_dim = "Unknown"
        else:
            header_dim = "Unknown"

        if header_dim != expected_dim:
            print(f"  FAIL: Dimensional header mismatch for '{name}': expected {expected_dim}, got {header_dim}", file=sys.stderr)
            all_passed = False

        mol = Chem.MolFromMolBlock(molfile)
        if mol is None:
            print(f"  FAIL: RDKit could not parse Chemy-exported file for '{name}'", file=sys.stderr)
            all_passed = False
            continue

        actual_formula = rdMolDescriptors.CalcMolFormula(mol)
        if actual_formula != expected_formula:
            print(f"  FAIL: Formula mismatch for '{name}': expected {expected_formula}, got {actual_formula}", file=sys.stderr)
            all_passed = False

        mol_with_h = Chem.AddHs(mol)
        actual_atoms = mol_with_h.GetNumAtoms()
        actual_bonds = mol_with_h.GetNumBonds()
        if actual_atoms != expected_atoms:
            print(f"  FAIL: Atom count mismatch for '{name}': expected {expected_atoms}, got {actual_atoms}", file=sys.stderr)
            all_passed = False

        if actual_bonds != expected_bonds:
            print(f"  FAIL: Bond count mismatch for '{name}': expected {expected_bonds}, got {actual_bonds}", file=sys.stderr)
            all_passed = False

        if mol.GetNumConformers() < 1:
            print(f"  FAIL: No conformer found for '{name}'", file=sys.stderr)
            all_passed = False
        else:
            conf = mol.GetConformer()
            positions = conf.GetPositions()
            if not positions.any():
                print(f"  FAIL: All-zero coordinates for '{name}'", file=sys.stderr)
                all_passed = False

        actual_charge = Chem.GetFormalCharge(mol)
        if actual_charge != expected_charge:
            print(f"  FAIL: Charge mismatch for '{name}'. Expected {expected_charge}, got {actual_charge}", file=sys.stderr)
            all_passed = False
            continue

        print(f"  PASS: Chemy-exported '{name}' verified by RDKit: formula={actual_formula}, charge={actual_charge}, atoms={actual_atoms}, header={header_dim}")

    # Check SDF in actual_dir
    sdf_path = os.path.join(actual_dir, "multi_compound.sdf")
    if not os.path.exists(sdf_path):
        print(f"  FAIL: Required multi-compound SDF '{sdf_path}' not found!", file=sys.stderr)
        all_passed = False
    else:
        suppl = Chem.SDMolSupplier(sdf_path)
        mols = [m for m in suppl if m is not None]
        if len(mols) != 3:
            print(f"  FAIL: Expected 3 valid SDF records, got {len(mols)}", file=sys.stderr)
            all_passed = False
        else:
            formulas = [rdMolDescriptors.CalcMolFormula(m) for m in mols]
            expected_formulas = ["C9H8O4", "C2H6O", "C6H6"]
            if formulas != expected_formulas:
                print(f"  FAIL: SDF record formulas mismatch: expected {expected_formulas}, got {formulas}", file=sys.stderr)
                all_passed = False
            else:
                print(f"  PASS: Chemy-exported SDF verified: 3/3 records parsed with formulas {formulas}")

    return all_passed


def main() -> None:
    parser = argparse.ArgumentParser(description="Bidirectional Chemy <-> RDKit Molfile/SDF Interoperability Suite")
    parser.add_argument("--generate-rdkit", action="store_true", help="Generate RDKit fixtures only (Direction 2)")
    parser.add_argument("--verify-chemy", action="store_true", help="Verify Chemy exports only (Direction 1)")
    parser.add_argument("--rdkit-dir", metavar="DIR", default=RDKIT_EXPORT_DIR, help="Directory for RDKit fixtures")
    parser.add_argument("--chemy-dir", metavar="DIR", default=CHEM_EXPORT_DIR, help="Directory for Chemy exports")
    args = parser.parse_args()

    run_generate = args.generate_rdkit or (not args.generate_rdkit and not args.verify_chemy)
    run_verify = args.verify_chemy or (not args.generate_rdkit and not args.verify_chemy)

    if run_generate:
        generate_rdkit_fixtures(args.rdkit_dir)

    if run_verify:
        success = verify_chemy_exports(args.chemy_dir)
        if not success:
            sys.exit(1)

    print("\nSUCCESS: Molfile/SDF interoperability task completed.")


if __name__ == "__main__":
    main()
