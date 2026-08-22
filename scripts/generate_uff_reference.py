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


def _build_ammonia() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("N"))
    coords = [
        (0.0, 0.0, 0.0),
        (0.939, 0.0, -0.377),
        (-0.470, 0.813, -0.377),
        (-0.470, -0.813, -0.377)
    ]
    conf = Chem.Conformer(4)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_phosphine() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("P"))
    coords = [
        (0.0, 0.0, 0.0),
        (1.1923, 0.0, -0.7712),
        (-0.5962, 1.0326, -0.7712),
        (-0.5962, -1.0326, -0.7712)
    ]
    conf = Chem.Conformer(4)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_bromomethane() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("CBr"))
    coords = [
        (0.0, 0.0, 0.0),      # C
        (1.94, 0.0, 0.0),      # Br
        (-0.36, 1.02, 0.0),    # H
        (-0.36, -0.51, 0.89),  # H
        (-0.36, -0.51, -0.89)  # H
    ]
    conf = Chem.Conformer(5)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_iodomethane() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("CI"))
    coords = [
        (0.0, 0.0, 0.0),      # C
        (2.16, 0.0, 0.0),      # I
        (-0.36, 1.02, 0.0),    # H
        (-0.36, -0.51, 0.89),  # H
        (-0.36, -0.51, -0.89)  # H
    ]
    conf = Chem.Conformer(5)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_formamide() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("C(=O)N"))
    coords = [
        (0.0, 0.0, 0.0),         # C
        (1.22, 0.0, 0.0),        # O
        (-0.68, 1.18, 0.0),      # N (planar sp2)
        (-0.55, -0.953, 0.0),    # H(formyl)
        (-0.175, 2.055, 0.0),    # H1(amide)
        (-1.69, 1.18, 0.0)       # H2(amide)
    ]
    conf = Chem.Conformer(6)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_methanol() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("CO"))
    coords = [
        (-0.3698, 0.0026, 0.0028),
        (0.898, -0.5748, -0.1191),
        (-0.6741, -0.1194, 1.0508),
        (-0.3142, 1.0665, -0.3155),
        (-1.083, -0.5246, -0.643),
        (1.5432, 0.1497, 0.024)
    ]
    conf = Chem.Conformer(6)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_acetone() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("CC(=O)C"))
    coords = [
        (-1.2921, 0.094, 0.0502),
        (0.0407, -0.0575, -0.5919),
        (0.1083, -0.1835, -1.8178),
        (1.2863, -0.0553, 0.257),
        (-1.8439, 0.9295, -0.4098),
        (-1.1567, 0.3216, 1.1165),
        (-1.8581, -0.8567, -0.0294),
        (1.308, -1.0314, 0.7436),
        (2.184, 0.0957, -0.3374),
        (1.2235, 0.7437, 1.0189)
    ]
    conf = Chem.Conformer(10)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_toluene() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("Cc1ccccc1"))
    coords = [
        (2.1804, -0.165, 0.0752),
        (0.7078, -0.0235, 0.0201),
        (0.0736, 1.198, 0.1285),
        (-1.301, 1.319, 0.0758),
        (-2.0273, 0.1606, -0.0912),
        (-1.4224, -1.0903, -0.2044),
        (-0.0254, -1.1889, -0.148),
        (2.4281, -0.6299, 1.0537),
        (2.6489, 0.8197, 0.0482),
        (2.5634, -0.8913, -0.6749),
        (0.6773, 2.0978, 0.2602),
        (-1.7888, 2.2867, 0.1624),
        (-3.0899, 0.2465, -0.1328),
        (-2.0224, -1.9779, -0.3345),
        (0.3975, -2.1616, -0.2383)
    ]
    conf = Chem.Conformer(15)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_pyridine() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("c1ccncc1"))
    coords = [
        (-0.1131, 1.1762, 0.0098),
        (-1.2281, 0.3529, 0.01),
        (-1.0897, -1.0219, -0.0016),
        (0.1271, -1.5612, -0.0129),
        (1.2543, -0.831, -0.0138),
        (1.1348, 0.5505, -0.0024),
        (-0.1737, 2.2543, 0.0186),
        (-2.2125, 0.8149, 0.0193),
        (-1.9904, -1.629, -0.001),
        (2.2322, -1.2518, -0.0229),
        (2.0591, 1.146, -0.0031)
    ]
    conf = Chem.Conformer(11)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_dichloromethane() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("ClCCl"))
    coords = [
        (1.4555, 0.8698, 0.0164),
        (0.0034, -0.139, -0.0093),
        (-1.4527, 0.8722, -0.0555),
        (0.0553, -0.8463, -0.8678),
        (-0.0614, -0.7567, 0.9163)
    ]
    conf = Chem.Conformer(5)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_furan() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("c1ccoc1"))
    coords = [
        (-0.6894, 0.7099, -0.3354),
        (0.6965, 0.702, -0.2795),
        (1.0387, -0.6292, -0.1963),
        (-0.0099, -1.3983, -0.1982),
        (-1.0602, -0.6179, -0.281),
        (-1.3305, 1.5821, -0.4066),
        (1.3446, 1.5542, -0.298),
        (2.0848, -0.9431, -0.138),
        (-2.0745, -0.9597, -0.3038)
    ]
    conf = Chem.Conformer(9)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_thiophene() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("c1ccsc1"))
    coords = [
        (-0.6517, -0.6596, 0.0211),
        (0.6659, -0.6529, -0.0859),
        (1.3252, 0.5458, -0.0815),
        (-0.0248, 1.8049, 0.0896),
        (-1.3173, 0.5566, 0.1343),
        (-1.2684, -1.5869, 0.0264),
        (1.2688, -1.5902, -0.1805),
        (2.373, 0.8343, -0.1529),
        (-2.3708, 0.7481, 0.2295)
    ]
    conf = Chem.Conformer(9)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def _build_acetonitrile() -> Chem.Mol:
    mol = Chem.AddHs(Chem.MolFromSmiles("CC#N"))
    coords = [
        (-0.4891, 0.0095, -0.0117),
        (0.9717, -0.0107, 0.0103),
        (2.13, -0.0385, 0.0405),
        (-0.8982, 0.0003, 1.0033),
        (-0.8473, 0.9398, -0.5146),
        (-0.8671, -0.9004, -0.5278)
    ]
    conf = Chem.Conformer(6)
    for idx, pos in enumerate(coords):
        conf.SetAtomPosition(idx, pos)
    mol.AddConformer(conf, assignId=True)
    return mol


def generate_reference() -> dict:
    """Calculate UFF energies for butane conformers, development molecules, and held-out evaluation molecules."""
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
        "expanded_regression_molecules": {},
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

    # 2. Diverse Molecules (hybridizations, oxygen, sulfur, nitrogen, phosphorus, halogens, amides)
    diverse_builders = [
        ("methane", "C", _build_methane()),
        ("ethane", "CC", _build_ethane()),
        ("ethylene", "C=C", _build_ethylene()),
        ("water", "O", _build_water()),
        ("h2s", "S", _build_h2s()),
        ("chloromethane", "CCl", _build_chloromethane()),
        ("fluoromethane", "CF", _build_fluoromethane()),
        ("ammonia", "N", _build_ammonia()),
        ("phosphine", "P", _build_phosphine()),
        ("bromomethane", "CBr", _build_bromomethane()),
        ("iodomethane", "CI", _build_iodomethane()),
        ("formamide", "C(=O)N", _build_formamide()),
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

    # 3. Post-development expanded regression molecules.
    # This partition detects numerical drift; it is not a prospective or blind holdout.
    expanded_regression_builders = [
        ("methanol", "CO", _build_methanol()),
        ("acetone", "CC(=O)C", _build_acetone()),
        ("toluene", "Cc1ccccc1", _build_toluene()),
        ("pyridine", "c1ccncc1", _build_pyridine()),
        ("dichloromethane", "ClCCl", _build_dichloromethane()),
        ("furan", "c1ccoc1", _build_furan()),
        ("thiophene", "c1ccsc1", _build_thiophene()),
        ("acetonitrile", "CC#N", _build_acetonitrile()),
    ]

    for name, smiles, mol in expanded_regression_builders:
        ff = rdForceFieldHelpers.UFFGetMoleculeForceField(mol)
        assert ff is not None, f"UFF parameterization failed for {name}"
        energy = ff.CalcEnergy()
        results["expanded_regression_molecules"][name] = {
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
        for name in ["methane", "ethane", "ethylene", "water", "h2s", "chloromethane", "fluoromethane", "ammonia", "phosphine", "bromomethane", "iodomethane", "formamide"]:
            stored = existing["diverse_molecules"][name]["uff_total_kcal_mol"]
            fresh = ref["diverse_molecules"][name]["uff_total_kcal_mol"]
            if abs(stored - fresh) > 1e-3:
                print(f"FAIL: {name} energy mismatch: stored={stored}, fresh={fresh}", file=sys.stderr)
                sys.exit(1)

        # Compare post-development expanded-regression molecule energies
        for name in ["methanol", "acetone", "toluene", "pyridine", "dichloromethane", "furan", "thiophene", "acetonitrile"]:
            stored = existing["expanded_regression_molecules"][name]["uff_total_kcal_mol"]
            fresh = ref["expanded_regression_molecules"][name]["uff_total_kcal_mol"]
            if abs(stored - fresh) > 1e-3:
                print(f"FAIL: {name} energy mismatch: stored={stored}, fresh={fresh}", file=sys.stderr)
                sys.exit(1)

        print(f"PASS: All 4 butane conformers, 12 diverse molecules, and 8 expanded-regression molecules reproduced with RDKit {RDKIT_VERSION} UFF")
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
