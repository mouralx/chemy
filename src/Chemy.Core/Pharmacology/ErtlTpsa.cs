namespace Chemy.Core.Pharmacology;

using Chemy.Core.Graph;
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
)
{
    public ScientificApplicabilityAssessment Applicability { get; init; } = new(
        ApplicabilityStatus.OutOfDomain,
        ["Applicability was not evaluated."]);

    public ScientificUncertainty Uncertainty { get; init; } = new(
        0.53,
        "angstrom^2",
        1.0,
        "Maximum observed absolute agreement error across the pinned 48-molecule RDKit benchmark; not an experimental confidence interval.",
        "chemy-rdkit-descriptor-benchmark-v2.8");
}

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
/// Ertl-Inspired Topological Polar Surface Area (TPSA) calculator based on polar atom fragment contributions.
/// Reference: Ertl, P., Rohde, B., &amp; Selzer, P. (2000). Fast calculation of molecular polar surface area 
/// as a sum of fragment-based contributions and its application to the prediction of drug transport properties. 
/// Journal of Medicinal Chemistry, 43(20), 3714-3717.
/// </summary>
public static class ErtlTpsa
{
    private static readonly IReadOnlySet<string> SupportedElements = new HashSet<string>(
        ["H", "C", "N", "O", "P", "S", "F", "Cl", "Br", "I"],
        StringComparer.Ordinal);

    private static readonly ScientificMethodInfo TpsaMethodInfo = new(
        "Ertl-Inspired Topological Polar Surface Area (Fragment Subset)",
        "2000.1",
        EvidenceLevel.EmpiricalModel,
        "Organic small molecules containing polar N, O, P, S fragments.",
        [
            "Fragment-based polar surface area contribution model.",
            "Unsupported elements fail closed; zero contributions are retained only for published zero-area fragments."
        ]
    )
    {
        ReferenceUris = ["https://doi.org/10.1021/jm000942e"],
        ValidationEvidence = new ScientificValidationEvidence(
            "chemy-rdkit-descriptor-benchmark-v2.8",
            "2.8",
            48,
            [
                new("MAE", 0.0110, "angstrom^2"),
                new("RMSE", 0.0765, "angstrom^2"),
                new("MaximumAbsoluteError", 0.5300, "angstrom^2")
            ],
            "src/Chemy.Core.Tests/ValidationData/reference_compounds.json",
            "3d579feb7fbe159de194764556f0f31821cd69ffedee90e19a6165889b9452c5",
            false,
            false)
    };

    /// <summary>
    /// Computes the Ertl Topological Polar Surface Area (TPSA) for a molecule.
    /// </summary>
    public static TpsaResult Calculate(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        if (!molecule.HasBondedTopology)
        {
            throw new InvalidOperationException(
                $"Molecule '{molecule.Name}' has no bonded topology. TPSA calculation requires a bonded molecular graph (e.g. from SMILES or Molfile/SDF), not an empirical formula without connectivity.");
        }

        var applicability = ScientificApplicability.AssessMolecule(molecule, SupportedElements);
        ScientificApplicability.RequireWithinDomain(applicability, TpsaMethodInfo.Method);

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

        return new TpsaResult(Math.Round(total, 2), contributions, TpsaMethodInfo)
        {
            Applicability = applicability
        };
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
                if (isAromatic)
                {
                    // :O: (Aromatic ring oxygen, e.g. furan)
                    return (":O: (Aromatic ring oxygen)", 13.14);
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

                    // N-substituted aromatic nitrogen (e.g. N-alkyl pyrrole, caffeine N-methyl imidazole)
                    int nonAromaticBonds = molecule.Bonds.Count(b => b.Connects(atomIndex) && b.Type != BondType.Aromatic);
                    if (nonAromaticBonds > 0 && doubleBonds == 0)
                    {
                        return (":NR: (Aromatic substituted nitrogen)", 8.31);
                    }

                    if (doubleBonds >= 1 || aromaticBonds >= 2)
                    {
                        // Pyridine / Pyrimidine :N:
                        return (":N: (Aromatic pyridyl nitrogen)", 12.89);
                    }
                    return (":N: (Aromatic nitrogen)", 12.89);
                }

                bool inRing = IsInRing(molecule, atomIndex);

                if (doubleBonds >= 1)
                {
                    // =N- Imine, azo, heterocyclic =N-
                    if (hCount >= 1)
                    {
                        return ("=NH (Imine nitrogen with H)", 23.85);
                    }
                    if (inRing)
                    {
                        return (":N: (Heterocyclic ring nitrogen)", 12.89);
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

                if (inRing && doubleBonds == 0 && hCount == 0)
                {
                    // Check if adjacent to double-bonded carbon in conjugated ring (e.g. N-methyl imidazole in purines)
                    bool isConjugatedRing = neighbors.Any(n => molecule.Bonds.Any(b => b.Connects(n) && b.Type == BondType.Double));
                    if (isConjugatedRing)
                    {
                        return (":NR: (Heterocyclic planar nitrogen)", 8.31);
                    }
                }

                if (in3MemberedRing)
                {
                    return hCount switch
                    {
                        >= 1 => ("-NH- (Aziridine 3-ring nitrogen with H)", 8.25),
                        _ => ("-NR- (Aziridine 3-ring tertiary nitrogen)", 3.01)
                    };
                }

                // Standard aliphatic amines
                return hCount switch
                {
                    >= 2 => ("-NH2 (Primary amine nitrogen)", 26.02),
                    1 => ("-NH- (Secondary amine nitrogen)", 12.03),
                    _ => ("-NR2 (Tertiary amine nitrogen)", 3.24)
                };

            case "S":
                // 3. Sulfur Fragments (Ertl Table 1)
                if (isAromatic)
                {
                    // :S: (Aromatic ring sulfur, e.g. thiophene)
                    return (":S: (Aromatic ring sulfur)", 0.0);
                }
                int sOxygens = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "O");
                if (sOxygens >= 1)
                {
                    // Sulfoxide / Sulfone sulfur atom (polar area is carried by double-bonded oxygens)
                    return ("-SO- / -SO2- (Sulfoxide/Sulfone sulfur)", 0.0);
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
                if (pOxygens >= 3)
                {
                    // Phosphate / Phosphonate phosphorus (polar area carried by oxygens)
                    return ("-PO4- (Phosphate phosphorus)", 0.0);
                }
                if (doubleBonds >= 1)
                {
                    return ("=P- (Phosphorus with double bond)", 9.81);
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

    private static bool IsInRing(Molecule molecule, int atomIndex)
    {
        var sssr = CycleBasis.ComputeSssr(molecule);
        return sssr.Rings.Any(r => r.Contains(atomIndex));
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
