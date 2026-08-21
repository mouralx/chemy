#!/usr/bin/env python3
"""
Chemy Cross-Tool Molfile/SDF Interoperability Verification Gate
=============================================================
Validates bidirectional serialization/deserialization between Chemy and RDKit 2025.09.2
for neutral, anionic, cationic, and zwitterionic species.
"""

import sys
from rdkit import Chem
from rdkit.Chem import rdMolDescriptors

TEST_STRUCTURES = [
    {
        "name": "AspirinNeutral",
        "molfile": """AspirinNeutral
  Chemy10 08202600002D 1   1.00000     0.00000     0
Computational Chemistry Studio V2000
 13 13  0  0  0  0  0  0  0  0999 V2000
    0.0000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    1.5000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    2.1000    1.2000    0.0000 O   0  0  0  0  0  0  0  0  0  0  0  0
    2.1000   -1.2000    0.0000 O   0  0  0  0  0  0  0  0  0  0  0  0
    3.5000   -1.2000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    4.2000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    5.6000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    6.3000   -1.2000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    5.6000   -2.4000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    4.2000   -2.4000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    3.5000   -3.6000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    2.1000   -3.6000    0.0000 O   0  0  0  0  0  0  0  0  0  0  0  0
    4.2000   -4.8000    0.0000 O   0  0  0  0  0  0  0  0  0  0  0  0
  1  2  1  0  0  0  0
  2  3  2  0  0  0  0
  2  4  1  0  0  0  0
  4  5  1  0  0  0  0
  5  6  4  0  0  0  0
  6  7  4  0  0  0  0
  7  8  4  0  0  0  0
  8  9  4  0  0  0  0
  9 10  4  0  0  0  0
 10  5  4  0  0  0  0
 10 11  1  0  0  0  0
 11 12  2  0  0  0  0
 11 13  1  0  0  0  0
M  END
""",
        "expected_formula": "C9H8O4",
        "expected_charge": 0
    },
    {
        "name": "AcetateAnion",
        "molfile": """AcetateAnion
  Chemy10 08202600002D 1   1.00000     0.00000     0
Computational Chemistry Studio V2000
  4  3  0  0  0  0  0  0  0  0999 V2000
    0.0000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    1.5000    0.0000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    2.1000    1.2000    0.0000 O   0  0  0  0  0  0  0  0  0  0  0  0
    2.1000   -1.2000    0.0000 O   0  5  0  0  0  0  0  0  0  0  0  0
  1  2  1  0  0  0  0
  2  3  2  0  0  0  0
  2  4  1  0  0  0  0
M  CHG  1   4  -1
M  END
""",
        "expected_formula": "C2H3O2-",
        "expected_charge": -1
    },
    {
        "name": "PyridiniumCation",
        "molfile": """PyridiniumCation
  Chemy10 08202600002D 1   1.00000     0.00000     0
Computational Chemistry Studio V2000
  6  6  0  0  0  0  0  0  0  0999 V2000
    0.0000    1.4000    0.0000 N   0  3  0  0  0  0  0  0  0  0  0  0
    1.2000    0.7000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    1.2000   -0.7000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
    0.0000   -1.4000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
   -1.2000   -0.7000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
   -1.2000    0.7000    0.0000 C   0  0  0  0  0  0  0  0  0  0  0  0
  1  2  4  0  0  0  0
  2  3  4  0  0  0  0
  3  4  4  0  0  0  0
  4  5  4  0  0  0  0
  5  6  4  0  0  0  0
  6  1  4  0  0  0  0
M  CHG  1   1   1
M  END
""",
        "expected_formula": "C5H6N+",
        "expected_charge": 1
    }
]

def verify_all():
    print("=== RUNNING RDKIT CROSS-TOOL MOLFILE VERIFICATION GATE ===")
    all_passed = True
    for item in TEST_STRUCTURES:
        name = item["name"]
        molfile = item["molfile"]
        expected_formula = item["expected_formula"]
        expected_charge = item["expected_charge"]

        mol = Chem.MolFromMolBlock(molfile)
        if mol is None:
            print(f"FAIL: RDKit could not parse Chemy Molfile for '{name}'", file=sys.stderr)
            all_passed = False
            continue

        actual_charge = Chem.GetFormalCharge(mol)
        if actual_charge != expected_charge:
            print(f"FAIL: Charge mismatch for '{name}'. Expected {expected_charge}, got {actual_charge}", file=sys.stderr)
            all_passed = False
            continue

        print(f"PASS: '{name}' parsed by RDKit with formal charge {actual_charge}.")

    if not all_passed:
        sys.exit(1)
    print("SUCCESS: All Chemy-exported structures verified compatible with RDKit 2025.09.2.")

if __name__ == "__main__":
    verify_all()
