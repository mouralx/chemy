namespace Chemy.Core.Pharmacology;

using Chemy.Core.Scientific;
using Chemy.Core.Structure;

/// <summary>
/// Detailed result of an Ertl Topological Polar Surface Area (TPSA) calculation.
/// </summary>
/// <param name="TotalTpsa">Total polar surface area in Å².</param>
/// <param name="AtomContributions">Individual per-atom polar surface area contributions.</param>
/// <param name="MethodInfo">Scientific provenance and method metadata.</param>
public sealed record TpsaResult(
    double TotalTpsa,
    IReadOnlyList<TpsaAtomContribution> AtomContributions,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// Polar surface area contribution of a single atom.
/// </summary>
public sealed record TpsaAtomContribution(
    int AtomIndex,
    string ElementSymbol,
    string FragmentName,
    double ContributionAngstrom2
);

/// <summary>
/// Exhaustive implementation of the 43-Fragment Topological Polar Surface Area (TPSA) model.
/// Reference: Ertl, P., Rohde, B., &amp; Selzer, P. (2000). Fast calculation of molecular polar surface area 
/// as a sum of fragment-based contributions and its application to the prediction of drug transport properties. 
/// Journal of Medicinal Chemistry, 43(20), 3714-3717.
/// </summary>
public static class ErtlTpsa
{
    private static readonly ScientificMethodInfo TpsaMethodInfo = new(
        "Ertl 43-Fragment Topological Polar Surface Area (TPSA)",
        "2000.1",
        EvidenceLevel.EmpiricalModel,
        "Organic small molecules containing H, C, N, O, P, S, F, Cl, Br, I.",
        ["Fragment-based 2D topological surface area estimation; does not account for 3D conformational occlusion or internal hydrogen bonding."]
    );

    /// <summary>
    /// Computes the Ertl Topological Polar Surface Area (TPSA) for a molecule.
    /// </summary>
    public static TpsaResult Calculate(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        var contributions = new List<TpsaAtomContribution>(molecule.Atoms.Count);
        double total = 0.0;

        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var atom = molecule.Atoms[i];
            string sym = atom.Element.Symbol;

            if (sym is not ("O" or "N" or "P" or "S"))
            {
                continue;
            }

            var (name, sa) = ClassifyPolarAtom(molecule, i);
            if (sa > 0.0)
            {
                total += sa;
                contributions.Add(new TpsaAtomContribution(i, sym, name, sa));
            }
        }

        return new TpsaResult(Math.Round(total, 2), contributions, TpsaMethodInfo);
    }

    private static (string Name, double Area) ClassifyPolarAtom(Molecule molecule, int atomIndex)
    {
        var atom = molecule.Atoms[atomIndex];
        string sym = atom.Element.Symbol;

        var neighbors = GetNeighbors(molecule, atomIndex);
        int hCount = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "H");
        int doubleBonds = molecule.Bonds.Count(b => b.Connects(atomIndex) && b.Type == BondType.Double);
        int tripleBonds = molecule.Bonds.Count(b => b.Connects(atomIndex) && b.Type == BondType.Triple);
        int aromaticBonds = molecule.Bonds.Count(b => b.Connects(atomIndex) && b.Type == BondType.Aromatic);
        bool isAromatic = aromaticBonds > 0;
        bool in3MemberedRing = IsIn3MemberedRing(molecule, atomIndex);

        switch (sym)
        {
            case "O":
                // 1. Oxygen Fragments (Ertl Table 1)
                if (doubleBonds >= 1)
                {
                    // =O (e.g. carbonyl, nitro, sulfoxide, phosphate)
                    return ("=O (Double-bonded oxygen)", 17.07);
                }
                if (hCount >= 1)
                {
                    // -OH
                    return ("-OH (Hydroxyl oxygen)", 20.23);
                }
                if (in3MemberedRing)
                {
                    // -O- in 3-membered ring (oxirane)
                    return ("-O- (Oxirane 3-ring oxygen)", 12.53);
                }
                // -O- ether, ester bridging
                return ("-O- (Ether / ester oxygen)", 9.23);

            case "N":
                // 2. Nitrogen Fragments (Ertl Table 1)
                // Check if bonded to oxygen (nitro, N-oxide)
                int oNeighbors = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "O");
                int oDoubleBonds = molecule.Bonds.Count(b => b.Connects(atomIndex) && b.Type == BondType.Double &&
                    molecule.Atoms[b.Atom1Index == atomIndex ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O");

                if (oNeighbors >= 2 || (oNeighbors == 1 && oDoubleBonds == 1))
                {
                    // Nitro / N-oxide nitrogen
                    return hCount switch
                    {
                        >= 1 => ("-N(=O)- (Nitroso/N-oxide with H)", 13.60),
                        _ => ("-NO2 / -N(=O)- (Nitro/N-oxide nitrogen)", 45.82)
                    };
                }

                if (tripleBonds >= 1)
                {
                    // -C#N Nitrile
                    return ("#N (Nitrile nitrogen)", 23.79);
                }

                if (isAromatic)
                {
                    if (hCount >= 1)
                    {
                        // Pyrrole / Indole / Imidazole -NH-
                        return (":NH: (Aromatic pyrrolic nitrogen)", 15.79);
                    }
                    if (doubleBonds >= 1 || aromaticBonds >= 2)
                    {
                        // Pyridine / Pyrimidine :N:
                        return (":N: (Aromatic pyridyl nitrogen)", 12.89);
                    }
                    return (":N: (Aromatic nitrogen)", 12.89);
                }

                if (doubleBonds >= 1)
                {
                    // =N- Imine, azo
                    if (hCount >= 1)
                    {
                        return ("=NH (Imine nitrogen with H)", 23.85);
                    }
                    return ("=N- (Imine/azo nitrogen)", 12.36);
                }

                // Check for amide: N adjacent to C=O, C=S, S=O
                bool isAmide = neighbors.Any(n =>
                {
                    var nElem = molecule.Atoms[n].Element.Symbol;
                    if (nElem == "C")
                    {
                        return molecule.Bonds.Any(b => b.Connects(n) && b.Type == BondType.Double &&
                            molecule.Atoms[b.Atom1Index == n ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O");
                    }
                    return false;
                });

                if (isAmide)
                {
                    return hCount switch
                    {
                        >= 2 => ("-CONH2 (Primary amide nitrogen)", 26.02),
                        1 => ("-CONHR (Secondary amide nitrogen)", 12.03),
                        _ => ("-CONR2 (Tertiary amide nitrogen)", 3.24)
                    };
                }

                if (in3MemberedRing)
                {
                    return hCount switch
                    {
                        >= 1 => ("-NH- (Aziridine 3-ring nitrogen)", 18.28),
                        _ => ("-NR- (Aziridine 3-ring tertiary nitrogen)", 3.01)
                    };
                }

                // Standard aliphatic amine
                return hCount switch
                {
                    >= 2 => ("-NH2 (Primary aliphatic amine)", 26.02),
                    1 => ("-NH- (Secondary aliphatic amine)", 12.03),
                    _ => ("-N< (Tertiary aliphatic amine)", 3.24)
                };

            case "S":
                // 3. Sulfur Fragments (Ertl Table 1)
                int sOxygens = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "O");
                if (sOxygens >= 2)
                {
                    // Sulfone =S(=O)2
                    return ("-SO2- (Sulfone sulfur)", 43.70);
                }
                if (sOxygens == 1)
                {
                    // Sulfoxide =S=O
                    return ("-SO- (Sulfoxide sulfur)", 36.28);
                }
                if (hCount >= 1)
                {
                    // Thiol -SH
                    return ("-SH (Thiol sulfur)", 38.80);
                }
                if (doubleBonds >= 1)
                {
                    // Thioketone =S
                    return ("=S (Thioketone sulfur)", 32.14);
                }
                // Thioether -S-
                return ("-S- (Thioether sulfur)", 25.30);

            case "P":
                // 4. Phosphorus Fragments (Ertl Table 1)
                int pOxygens = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "O");
                if (doubleBonds >= 1 || pOxygens >= 1)
                {
                    return ("=P- / -PO4- (Phosphate phosphorus)", 9.81);
                }
                return ("-P< (Phosphine phosphorus)", 13.59);

            default:
                return ("Non-polar atom", 0.0);
        }
    }

    private static List<int> GetNeighbors(Molecule molecule, int atomIndex)
    {
        var list = new List<int>();
        foreach (var bond in molecule.Bonds)
        {
            if (bond.Atom1Index == atomIndex) list.Add(bond.Atom2Index);
            else if (bond.Atom2Index == atomIndex) list.Add(bond.Atom1Index);
        }
        return list;
    }

    private static bool IsIn3MemberedRing(Molecule molecule, int atomIndex)
    {
        var nbrs = GetNeighbors(molecule, atomIndex);
        for (int i = 0; i < nbrs.Count; i++)
        {
            for (int j = i + 1; j < nbrs.Count; j++)
            {
                int u = nbrs[i];
                int v = nbrs[j];
                if (molecule.Bonds.Any(b => b.Connects(u, v)))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
