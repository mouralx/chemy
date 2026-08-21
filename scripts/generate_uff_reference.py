#!/usr/bin/env python3
"""Generate and verify pinned RDKit UFF reference energies for butane conformers.

This script constructs explicit-hydrogen butane (C4H10) conformers at four
canonical dihedral angles (anti 180°, gauche 60°, eclipsed 120°, syn 0°)
using the EXACT same Cartesian coordinates used by the Chemy C# test
``Benchmark_ForceField_ButaneConformationalTorsionBarrier_MatchesRDKitUffReference``.

It evaluates UFF total energy via RDKit 2025.09.2 and writes a JSON artifact
with SHA-256 hash for CI reproducibility verification.

Requirements:
    pip install rdkit==2025.09.2

Usage:
    python3 scripts/generate_uff_reference.py                        # Generate reference JSON
    python3 scripts/generate_uff_reference.py --verify reference.json # Verify against existing
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import sys

import rdkit
from rdkit import Chem
from rdkit.Chem import AllChem, rdForceFieldHelpers


RDKIT_VERSION = "2025.09.2"
DIHEDRAL_ANGLES = [180.0, 60.0, 120.0, 0.0]
CONFORMER_NAMES = ["anti_180", "gauche_60", "eclipsed_120", "syn_0"]

# --- Coordinate generation (mirrors C# BuildButaneConformer exactly) ---

def _build_positions(phi_deg: float) -> list[tuple[float, float, float]]:
    """Build 14-atom all-hydrogen butane coordinates at dihedral phi_deg.

    Atom ordering: C1, C2, C3, C4, H1a, H1b, H1c, H2a, H2b, H3a, H3b, H4a, H4b, H4c
    This mirrors the C# ``BuildButaneConformer`` helper identically.
    """
    phi = math.radians(phi_deg)
    c, s = math.cos(phi), math.sin(phi)

    return [
        (-0.51,  1.44,       0.0),            # C1
        ( 0.0,   0.0,        0.0),            # C2
        ( 1.53,  0.0,        0.0),            # C3
        ( 2.04,  1.44 * c,   1.44 * s),       # C4
        (-1.55,  1.44,       0.0),            # H1a
        (-0.16,  1.95,       0.89),           # H1b
        (-0.16,  1.95,      -0.89),           # H1c
        (-0.36, -0.51,       0.89),           # H2a
        (-0.36, -0.51,      -0.89),           # H2b
        ( 1.89, -0.51 * c - 0.89 * s, -0.51 * s + 0.89 * c),  # H3a
        ( 1.89, -0.51 * c + 0.89 * s, -0.51 * s - 0.89 * c),  # H3b
        ( 3.08,  1.44 * c,   1.44 * s),       # H4a
        ( 1.69,  1.95 * c - 0.89 * s, 1.95 * s + 0.89 * c),   # H4b
        ( 1.69,  1.95 * c + 0.89 * s, 1.95 * s - 0.89 * c),   # H4c
    ]


def _build_rdkit_mol(phi_deg: float) -> Chem.Mol:
    """Create an RDKit Mol for butane with explicit H at the given dihedral."""
    mol = Chem.MolFromSmiles("CCCC")
    mol = Chem.AddHs(mol)
    conf = Chem.Conformer(14)
    for idx, (x, y, z) in enumerate(_build_positions(phi_deg)):
        conf.SetAtomPosition(idx, (x, y, z))
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_methane() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("C"))
    s = 1.09 / math.sqrt(3)
    coords = [
        (0.0, 0.0, 0.0),
        (s, s, s),
        (s, -s, -s),
        (-s, s, -s),
        (-s, -s, s)
    ]
    conf = Chem.Conformer(5)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_ethane() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("CC"))
    coords = [
        (0.0, 0.0, 0.0),
        (1.53, 0.0, 0.0),
        (-0.36, 1.02, 0.0),
        (-0.36, -0.51, 0.89),
        (-0.36, -0.51, -0.89),
        (1.89, -1.02, 0.0),
        (1.89, 0.51, 0.89),
        (1.89, 0.51, -0.89)
    ]
    conf = Chem.Conformer(8)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_ethylene() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("C=C"))
    coords = [
        (0.0, 0.0, 0.0),
        (1.34, 0.0, 0.0),
        (-0.55, 0.94, 0.0),
        (-0.55, -0.94, 0.0),
        (1.89, 0.94, 0.0),
        (1.89, -0.94, 0.0)
    ]
    conf = Chem.Conformer(6)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_water() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("O"))
    coords = [
        (0.0, 0.0, 0.0),
        (0.76, 0.59, 0.0),
        (-0.76, 0.59, 0.0)
    ]
    conf = Chem.Conformer(3)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_h2s() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("S"))
    coords = [
        (0.0, 0.0, 0.0),
        (0.963, 0.930, 0.0),
        (-0.963, 0.930, 0.0)
    ]
    conf = Chem.Conformer(3)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_chloromethane() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("CCl"))
    coords = [
        (0.0, 0.0, 0.0),      # C
        (1.78, 0.0, 0.0),      # Cl
        (-0.36, 1.02, 0.0),    # H
        (-0.36, -0.51, 0.89),  # H
        (-0.36, -0.51, -0.89)  # H
    ]
    conf = Chem.Conformer(5)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_fluoromethane() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("CF"))
    coords = [
        (0.0, 0.0, 0.0),      # C
        (1.39, 0.0, 0.0),      # F
        (-0.36, 1.02, 0.0),    # H
        (-0.36, -0.51, 0.89),  # H
        (-0.36, -0.51, -0.89)  # H
    ]
    conf = Chem.Conformer(5)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def generate_reference() -> dict:
    """Calculate UFF energies for butane conformers and diverse standard molecules."""
    assert rdkit.__version__ == RDKIT_VERSION, (
        f"RDKit version mismatch: expected {RDKIT_VERSION}, got {rdkit.__version__}"
    )

    results: dict = {
        "metadata": {
            "rdkit_version": RDKIT_VERSION,
            "force_field": "UFF",
            "coordinate_source": "Chemy exact geometry builders",
            "note": "Total energies are absolute UFF totals in kcal/mol.",
        },
        "butane_conformers": {},
        "diverse_molecules": {},
    }

    # 1. Butane Conformers
    anti_energy = None
    for name, phi in zip(CONFORMER_NAMES, DIHEDRAL_ANGLES):
        mol = _build_rdkit_mol(phi)
        ff = rdForceFieldHelpers.UFFGetMoleculeForceField(mol)
        assert ff is not None, f"UFF parameterization failed for {name}"
        energy = ff.CalcEnergy()

        if phi == 180.0:
            anti_energy = energy

        results["butane_conformers"][name] = {
            "dihedral_deg": phi,
            "uff_total_kcal_mol": round(energy, 4),
            "delta_vs_anti_kcal_mol": round(energy - anti_energy, 4) if anti_energy is not None else 0.0,
        }

    # 2. Diverse Molecules (hybridizations, oxygen, sulfur, halogens)
    diverse_builders = [
        ("methane", "C", _build_methane()),
        ("ethane", "CC", _build_ethane()),
        ("ethylene", "C=C", _build_ethylene()),
        ("water", "O", _build_water()),
        ("h2s", "S", _build_h2s()),
        ("chloromethane", "CCl", _build_chloromethane()),
        ("fluoromethane", "CF", _build_fluoromethane()),
    ]

    for name, smiles, mol in diverse_builders:
        ff = rdForceFieldHelpers.UFFGetMoleculeForceField(mol)
        assert ff is not None, f"UFF parameterization failed for {name}"
        energy = ff.CalcEnergy()
        results["diverse_molecules"][name] = {
            "smiles": smiles,
            "atom_count": mol.GetNumAtoms(),
            "uff_total_kcal_mol": round(energy, 4),
        }

    return results


def compute_hash(data: dict) -> str:
    """Compute SHA-256 hash of the canonical JSON representation."""
    canonical = json.dumps(data, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate/verify RDKit UFF butane reference energies")
    parser.add_argument("--verify", metavar="FILE", help="Verify against existing reference JSON")
    parser.add_argument("--output", metavar="FILE", default=None, help="Output JSON file path")
    args = parser.parse_args()

    ref = generate_reference()
    ref_hash = compute_hash(ref)
    ref["sha256"] = ref_hash

    if args.verify:
        with open(args.verify) as f:
            existing = json.load(f)
        existing_hash = existing.pop("sha256", None)
        recomputed_hash = compute_hash(existing)
        if existing_hash != recomputed_hash:
            print(f"FAIL: stored hash {existing_hash} != recomputed {recomputed_hash}", file=sys.stderr)
            sys.exit(1)

        # Compare butane energies
        for name in CONFORMER_NAMES:
            stored = existing["butane_conformers"][name]["uff_total_kcal_mol"]
            fresh = ref["butane_conformers"][name]["uff_total_kcal_mol"]
            if abs(stored - fresh) > 1e-3:
                print(f"FAIL: {name} energy mismatch: stored={stored}, fresh={fresh}", file=sys.stderr)
                sys.exit(1)

        # Compare diverse molecule energies
        for name in ["methane", "ethane", "ethylene", "water", "h2s", "chloromethane", "fluoromethane"]:
            stored = existing["diverse_molecules"][name]["uff_total_kcal_mol"]
            fresh = ref["diverse_molecules"][name]["uff_total_kcal_mol"]
            if abs(stored - fresh) > 1e-3:
                print(f"FAIL: {name} energy mismatch: stored={stored}, fresh={fresh}", file=sys.stderr)
                sys.exit(1)

        print(f"PASS: All 4 butane conformers and 7 diverse molecules verified against RDKit {RDKIT_VERSION} UFF")
        print(f"  SHA-256: {existing_hash}")
        return

    output = json.dumps(ref, indent=2)
    if args.output:
        with open(args.output, "w") as f:
            f.write(output + "\n")
        print(f"Written to {args.output}")
        print(f"  SHA-256: {ref_hash}")
    else:
        print(output)


if __name__ == "__main__":
    main()
