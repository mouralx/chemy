#!/usr/bin/env python3
"""
Chemy Independent Reference Dataset Generator
=============================================
Calculates and verifies the external chemoinformatics benchmark dataset (`reference_compounds.json`)
directly using authentic RDKit (2024.03+) and IUPAC CIAAW atomic weights.

Usage:
    python3 scripts/generate_reference_dataset.py --output src/Chemy.Core.Tests/ValidationData/reference_compounds.json
    python3 scripts/generate_reference_dataset.py --verify src/Chemy.Core.Tests/ValidationData/reference_compounds.json
"""

import hashlib
import json
import os
import sys

try:
    import rdkit
    from rdkit import Chem
    from rdkit.Chem import Crippen, Descriptors, Lipinski, QED, rdMolDescriptors
    RDKIT_AVAILABLE = True
except ImportError:
    RDKIT_AVAILABLE = False


# Reference compound list: (ID, Name, SMILES, Formula, ChEMBL/PubChem reference)
BENCHMARK_COMPOUNDS = [
    {
        "id": "CHEMBL25",
        "name": "Aspirin",
        "smiles": "CC(=O)Oc1ccccc1C(=O)O",
        "formula": "C9H8O4",
        "provenance": "ChEMBL25 / RDKit 2024.03+"
    },
    {
        "id": "CHEMBL521",
        "name": "Ibuprofen",
        "smiles": "CC(C)Cc1ccc(cc1)C(C)C(=O)O",
        "formula": "C13H18O2",
        "provenance": "ChEMBL521 / RDKit 2024.03+"
    },
    {
        "id": "CHEMBL112",
        "name": "Paracetamol",
        "smiles": "CC(=O)Nc1ccc(O)cc1",
        "formula": "C8H9NO2",
        "provenance": "ChEMBL112 / RDKit 2024.03+"
    },
    {
        "id": "CHEMBL113",
        "name": "Caffeine",
        "smiles": "CN1C=NC2=C1C(=O)N(C)C(=O)N2C",
        "formula": "C8H10N4O2",
        "provenance": "ChEMBL113 / RDKit 2024.03+"
    },
    {
        "id": "CHEMBL3",
        "name": "Nicotine",
        "smiles": "CN1CCCC1c2cccnc2",
        "formula": "C10H14N2",
        "provenance": "ChEMBL3 / RDKit 2024.03+"
    },
    {
        "id": "CID_241",
        "name": "Benzene",
        "smiles": "c1ccccc1",
        "formula": "C6H6",
        "provenance": "PubChem CID 241 / RDKit 2024.03+"
    },
    {
        "id": "CID_931",
        "name": "Naphthalene",
        "smiles": "c1ccc2ccccc2c1",
        "formula": "C10H8",
        "provenance": "PubChem CID 931 / RDKit 2024.03+"
    },
    {
        "id": "CID_1049",
        "name": "Pyridine",
        "smiles": "c1ccncc1",
        "formula": "C5H5N",
        "provenance": "PubChem CID 1049 / RDKit 2024.03+"
    },
    {
        "id": "CID_6115",
        "name": "Aniline",
        "smiles": "c1ccccc1N",
        "formula": "C6H7N",
        "provenance": "PubChem CID 6115 / RDKit 2024.03+"
    },
    {
        "id": "CID_243",
        "name": "BenzoicAcid",
        "smiles": "c1ccccc1C(=O)O",
        "formula": "C7H6O2",
        "provenance": "PubChem CID 243 / RDKit 2024.03+"
    },
    {
        "id": "CID_702",
        "name": "Ethanol",
        "smiles": "CCO",
        "formula": "C2H6O",
        "provenance": "PubChem CID 702 / RDKit 2024.03+"
    },
    {
        "id": "CID_180",
        "name": "Acetone",
        "smiles": "CC(=O)C",
        "formula": "C3H6O",
        "provenance": "PubChem CID 180 / RDKit 2024.03+"
    },
    {
        "id": "CID_176",
        "name": "AceticAcid",
        "smiles": "CC(=O)O",
        "formula": "C2H4O2",
        "provenance": "PubChem CID 176 / RDKit 2024.03+"
    },
    {
        "id": "CID_178",
        "name": "Acetamide",
        "smiles": "CC(=O)N",
        "formula": "C2H5NO",
        "provenance": "PubChem CID 178 / RDKit 2024.03+"
    },
    {
        "id": "CID_8857",
        "name": "EthylAcetate",
        "smiles": "CCOC(=O)C",
        "formula": "C4H8O2",
        "provenance": "PubChem CID 8857 / RDKit 2024.03+"
    },
    {
        "id": "CID_1176",
        "name": "Urea",
        "smiles": "NC(=O)N",
        "formula": "CH4N2O",
        "provenance": "PubChem CID 1176 / RDKit 2024.03+"
    }
]


def calculate_descriptors_with_rdkit(smiles: str):
    """Calculates authentic chemoinformatics descriptors directly using RDKit."""
    if not RDKIT_AVAILABLE:
        raise RuntimeError("RDKit is required to calculate reference descriptors dynamically.")

    mol = Chem.MolFromSmiles(smiles)
    if mol is None:
        raise ValueError(f"Failed to parse SMILES with RDKit: {smiles}")

    mw = round(Descriptors.MolWt(mol), 3)
    exact_mass = round(Descriptors.ExactMolWt(mol), 5)
    tpsa = round(rdMolDescriptors.CalcTPSA(mol), 2)
    logp = round(Crippen.MolLogP(mol), 2)
    qed_val = round(QED.qed(mol), 3)
    hbd = Lipinski.NumHDonors(mol)
    hba = Lipinski.NumHAcceptors(mol)
    rotb = Lipinski.NumRotatableBonds(mol)
    arom = Lipinski.NumAromaticRings(mol)

    return mw, exact_mass, tpsa, logp, qed_val, hbd, hba, rotb, arom


def generate_reference_dataset():
    """Generates the benchmark dataset records by invoking RDKit."""
    records = []
    rdkit_version = rdkit.__version__ if RDKIT_AVAILABLE else "2024.03+"

    for entry in BENCHMARK_COMPOUNDS:
        name = entry["name"]
        smiles = entry["smiles"]
        formula = entry["formula"]
        
        mw, exact_mass, tpsa, logp, qed_val, hbd, hba, rotb, arom = calculate_descriptors_with_rdkit(smiles)

        record = {
            "id": entry["id"],
            "name": name,
            "smiles": smiles,
            "formula": formula,
            "standardMolecularWeight": mw,
            "monoisotopicExactMass": exact_mass,
            "referenceTpsa": tpsa,
            "referenceLogP": logp,
            "referenceQed": qed_val,
            "referenceHbd": hbd,
            "referenceHba": hba,
            "referenceRotatableBonds": rotb,
            "referenceAromaticRings": arom,
            "provenance": entry["provenance"],
            "propertyProvenance": {
                "standardMolecularWeight": f"RDKit {rdkit_version} Descriptors.MolWt (IUPAC CIAAW)",
                "monoisotopicExactMass": f"RDKit {rdkit_version} Descriptors.ExactMolWt (NIST Physical Measurement Laboratory)",
                "referenceTpsa": f"RDKit {rdkit_version} rdMolDescriptors.CalcTPSA (Ertl et al. J. Med. Chem. 2000)",
                "referenceLogP": f"RDKit {rdkit_version} Crippen.MolLogP (Wildman & Crippen J. Chem. Inf. Comput. Sci. 1999)",
                "referenceQed": f"RDKit {rdkit_version} QED.qed (Bickerton et al. Nature Chem. 2012)",
                "referenceHbd": f"RDKit {rdkit_version} Lipinski.NumHDonors (Lipinski et al. Adv. Drug Deliv. Rev. 1997)",
                "referenceHba": f"RDKit {rdkit_version} Lipinski.NumHAcceptors (Lipinski et al. Adv. Drug Deliv. Rev. 1997)",
                "referenceRotatableBonds": f"RDKit {rdkit_version} Lipinski.NumRotatableBonds (Veber et al. J. Med. Chem. 2002)",
                "referenceAromaticRings": f"RDKit {rdkit_version} Lipinski.NumAromaticRings (Horton SSSR cycle basis)"
            }
        }
        records.append(record)

    return records


if __name__ == "__main__":
    if not RDKIT_AVAILABLE:
        print("ERROR: RDKit is not installed. Install via `pip install rdkit` to execute reference generator.", file=sys.stderr)
        sys.exit(1)

    output_path = "src/Chemy.Core.Tests/ValidationData/reference_compounds.json"
    verify_mode = False

    if len(sys.argv) > 1:
        if sys.argv[1] == "--output" and len(sys.argv) > 2:
            output_path = sys.argv[2]
        elif sys.argv[1] == "--verify":
            verify_mode = True
            if len(sys.argv) > 2:
                output_path = sys.argv[2]

    records = generate_reference_dataset()
    formatted_bytes = (json.dumps(records, indent=2) + "\n").encode("utf-8")
    actual_file_sha256 = hashlib.sha256(formatted_bytes).hexdigest()

    if verify_mode:
        if not os.path.exists(output_path):
            print(f"FAIL: Target file '{output_path}' does not exist.", file=sys.stderr)
            sys.exit(1)
        with open(output_path, "rb") as f:
            existing_bytes = f.read()
        if existing_bytes != formatted_bytes:
            print(f"FAIL: Target file '{output_path}' differs from RDKit {rdkit.__version__} calculated output.", file=sys.stderr)
            sys.exit(1)
        print(f"SUCCESS: '{output_path}' matches RDKit {rdkit.__version__} calculated values.")
        print(f"On-Disk File SHA-256: {actual_file_sha256}")
    else:
        with open(output_path, "wb") as f:
            f.write(formatted_bytes)
        print(f"Generated {len(records)} reference records using RDKit {rdkit.__version__} -> {output_path}")
        print(f"On-Disk File SHA-256: {actual_file_sha256}")
