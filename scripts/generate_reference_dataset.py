#!/usr/bin/env python3
"""
Chemy Independent Reference Dataset Generator
=============================================
Calculates and verifies the external chemoinformatics benchmark dataset (`reference_compounds.json`)
directly using authentic RDKit (2025.09.2) and IUPAC CIAAW atomic weights.

Usage:
    python3 scripts/generate_reference_dataset.py --output src/Chemy.Core.Tests/ValidationData/reference_compounds.json
    python3 scripts/generate_reference_dataset.py --verify src/Chemy.Core.Tests/ValidationData/reference_compounds.json
"""

import hashlib
import json
import os
import sys

PINNED_RDKIT_VERSION = "2025.09.2"

try:
    import rdkit
    from rdkit import Chem
    from rdkit.Chem import Crippen, Descriptors, Lipinski, QED, rdMolDescriptors
    RDKIT_AVAILABLE = True
except ImportError:
    RDKIT_AVAILABLE = False


# 32 Benchmark Compounds: 16 Tuning set + 16 Held-out Validation set (F, Cl, Br, S, P, Heterocycles, Polycycles)
BENCHMARK_COMPOUNDS = [
    # --- SUBSET 1: Core Tuning Benchmark (16 molecules) ---
    {
        "id": "CHEMBL25",
        "name": "Aspirin",
        "smiles": "CC(=O)Oc1ccccc1C(=O)O",
        "formula": "C9H8O4",
        "subset": "tuning",
        "provenance": f"ChEMBL25 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CHEMBL521",
        "name": "Ibuprofen",
        "smiles": "CC(C)Cc1ccc(cc1)C(C)C(=O)O",
        "formula": "C13H18O2",
        "subset": "tuning",
        "provenance": f"ChEMBL521 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CHEMBL112",
        "name": "Paracetamol",
        "smiles": "CC(=O)Nc1ccc(O)cc1",
        "formula": "C8H9NO2",
        "subset": "tuning",
        "provenance": f"ChEMBL112 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CHEMBL113",
        "name": "Caffeine",
        "smiles": "CN1C=NC2=C1C(=O)N(C)C(=O)N2C",
        "formula": "C8H10N4O2",
        "subset": "tuning",
        "provenance": f"ChEMBL113 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CHEMBL3",
        "name": "Nicotine",
        "smiles": "CN1CCCC1c2cccnc2",
        "formula": "C10H14N2",
        "subset": "tuning",
        "provenance": f"ChEMBL3 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_241",
        "name": "Benzene",
        "smiles": "c1ccccc1",
        "formula": "C6H6",
        "subset": "tuning",
        "provenance": f"PubChem CID 241 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_931",
        "name": "Naphthalene",
        "smiles": "c1ccc2ccccc2c1",
        "formula": "C10H8",
        "subset": "tuning",
        "provenance": f"PubChem CID 931 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_1049",
        "name": "Pyridine",
        "smiles": "c1ccncc1",
        "formula": "C5H5N",
        "subset": "tuning",
        "provenance": f"PubChem CID 1049 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_6115",
        "name": "Aniline",
        "smiles": "c1ccccc1N",
        "formula": "C6H7N",
        "subset": "tuning",
        "provenance": f"PubChem CID 6115 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_243",
        "name": "BenzoicAcid",
        "smiles": "c1ccccc1C(=O)O",
        "formula": "C7H6O2",
        "subset": "tuning",
        "provenance": f"PubChem CID 243 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_702",
        "name": "Ethanol",
        "smiles": "CCO",
        "formula": "C2H6O",
        "subset": "tuning",
        "provenance": f"PubChem CID 702 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_180",
        "name": "Acetone",
        "smiles": "CC(=O)C",
        "formula": "C3H6O",
        "subset": "tuning",
        "provenance": f"PubChem CID 180 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_176",
        "name": "AceticAcid",
        "smiles": "CC(=O)O",
        "formula": "C2H4O2",
        "subset": "tuning",
        "provenance": f"PubChem CID 176 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_178",
        "name": "Acetamide",
        "smiles": "CC(=O)N",
        "formula": "C2H5NO",
        "subset": "tuning",
        "provenance": f"PubChem CID 178 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_8857",
        "name": "EthylAcetate",
        "smiles": "CCOC(=O)C",
        "formula": "C4H8O2",
        "subset": "tuning",
        "provenance": f"PubChem CID 8857 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_1176",
        "name": "Urea",
        "smiles": "NC(=O)N",
        "formula": "CH4N2O",
        "subset": "tuning",
        "provenance": f"PubChem CID 1176 / RDKit {PINNED_RDKIT_VERSION}"
    },
    # --- SUBSET 2: Held-Out Chemical Space (Heteroatoms F, Cl, Br, S, P, Polycycles) (16 molecules) ---
    {
        "id": "CID_10008",
        "name": "Fluorobenzene",
        "smiles": "c1ccc(F)cc1",
        "formula": "C6H5F",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 10008 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_7964",
        "name": "Chlorobenzene",
        "smiles": "c1ccc(Cl)cc1",
        "formula": "C6H5Cl",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 7964 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_7961",
        "name": "Bromobenzene",
        "smiles": "c1ccc(Br)cc1",
        "formula": "C6H5Br",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 7961 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_7419",
        "name": "4-ChlorobenzoicAcid",
        "smiles": "O=C(O)c1ccc(Cl)cc1",
        "formula": "C7H5ClO2",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 7419 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_8030",
        "name": "Thiophene",
        "smiles": "c1ccsc1",
        "formula": "C4H4S",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 8030 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_8029",
        "name": "Furan",
        "smiles": "c1ccoc1",
        "formula": "C4H4O",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 8029 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_798",
        "name": "Indole",
        "smiles": "c1ccc2[nH]ccc2c1",
        "formula": "C8H7N",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 798 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_7047",
        "name": "Quinoline",
        "smiles": "c1ccc2ncccc2c1",
        "formula": "C9H7N",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 7047 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_8418",
        "name": "Anthracene",
        "smiles": "c1ccc2cc3ccccc3cc2c1",
        "formula": "C14H10",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 8418 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_995",
        "name": "Phenanthrene",
        "smiles": "c1ccc2c(c1)ccc3ccccc23",
        "formula": "C14H10",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 995 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_7067",
        "name": "Biphenyl",
        "smiles": "c1ccccc1-c2ccccc2",
        "formula": "C12H10",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 7067 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_679",
        "name": "DimethylSulfoxide",
        "smiles": "CS(=O)C",
        "formula": "C2H6OS",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 679 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_6395",
        "name": "MethanesulfonicAcid",
        "smiles": "CS(=O)(=O)O",
        "formula": "CH4O3S",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 6395 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_10672",
        "name": "TrimethylPhosphate",
        "smiles": "COP(=O)(OC)OC",
        "formula": "C3H9O4P",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 10672 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CID_6575",
        "name": "Trichloroethylene",
        "smiles": "ClC=C(Cl)Cl",
        "formula": "C2HCl3",
        "subset": "expanded_regression",
        "provenance": f"PubChem CID 6575 / RDKit {PINNED_RDKIT_VERSION}"
    },
    {
        "id": "CHEMBL22",
        "name": "Dapsone",
        "smiles": "Nc1ccc(S(=O)(=O)c2ccc(N)cc2)cc1",
        "formula": "C12H12N2O2S",
        "subset": "expanded_regression",
        "provenance": f"ChEMBL22 / RDKit {PINNED_RDKIT_VERSION}"
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

    for entry in BENCHMARK_COMPOUNDS:
        name = entry["name"]
        smiles = entry["smiles"]
        formula = entry["formula"]
        subset = entry["subset"]
        
        mw, exact_mass, tpsa, logp, qed_val, hbd, hba, rotb, arom = calculate_descriptors_with_rdkit(smiles)

        record = {
            "id": entry["id"],
            "name": name,
            "smiles": smiles,
            "formula": formula,
            "subset": subset,
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
                "standardMolecularWeight": f"RDKit {PINNED_RDKIT_VERSION} Descriptors.MolWt (IUPAC CIAAW)",
                "monoisotopicExactMass": f"RDKit {PINNED_RDKIT_VERSION} Descriptors.ExactMolWt (NIST Physical Measurement Laboratory)",
                "referenceTpsa": f"RDKit {PINNED_RDKIT_VERSION} rdMolDescriptors.CalcTPSA (Ertl et al. J. Med. Chem. 2000)",
                "referenceLogP": f"RDKit {PINNED_RDKIT_VERSION} Crippen.MolLogP (Wildman & Crippen J. Chem. Inf. Comput. Sci. 1999)",
                "referenceQed": f"RDKit {PINNED_RDKIT_VERSION} QED.qed (Bickerton et al. Nature Chem. 2012)",
                "referenceHbd": f"RDKit {PINNED_RDKIT_VERSION} Lipinski.NumHDonors (Lipinski et al. Adv. Drug Deliv. Rev. 1997)",
                "referenceHba": f"RDKit {PINNED_RDKIT_VERSION} Lipinski.NumHAcceptors (Lipinski et al. Adv. Drug Deliv. Rev. 1997)",
                "referenceRotatableBonds": f"RDKit {PINNED_RDKIT_VERSION} Lipinski.NumRotatableBonds (Veber et al. J. Med. Chem. 2002)",
                "referenceAromaticRings": f"RDKit {PINNED_RDKIT_VERSION} Lipinski.NumAromaticRings (Horton SSSR cycle basis)"
            }
        }
        records.append(record)

    return records


if __name__ == "__main__":
    if not RDKIT_AVAILABLE:
        print("ERROR: RDKit is not installed. Install via `pip install -r scripts/requirements-reference.txt`", file=sys.stderr)
        sys.exit(1)

    if rdkit.__version__ != PINNED_RDKIT_VERSION:
        print(f"ERROR: RDKit version mismatch. Expected {PINNED_RDKIT_VERSION}, found {rdkit.__version__}", file=sys.stderr)
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
            print(f"FAIL: Target file '{output_path}' differs from RDKit {PINNED_RDKIT_VERSION} calculated output.", file=sys.stderr)
            sys.exit(1)
        print(f"SUCCESS: '{output_path}' matches RDKit {PINNED_RDKIT_VERSION} calculated values ({len(records)} compounds).")
        print(f"On-Disk File SHA-256: {actual_file_sha256}")
    else:
        with open(output_path, "wb") as f:
            f.write(formatted_bytes)
        print(f"Generated {len(records)} reference records using RDKit {PINNED_RDKIT_VERSION} -> {output_path}")
        print(f"On-Disk File SHA-256: {actual_file_sha256}")
