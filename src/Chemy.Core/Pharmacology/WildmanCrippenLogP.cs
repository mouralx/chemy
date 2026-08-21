namespace Chemy.Core.Pharmacology;

using Chemy.Core.Scientific;
using Chemy.Core.Structure;

/// <summary>
/// Detailed result of a Wildman-Crippen LogP and Molar Refractivity (MR) calculation.
/// </summary>
/// <param name="CalculatedLogP">Calculated octanol-water partition coefficient (ALogP).</param>
/// <param name="CalculatedMr">Calculated Molar Refractivity (AMR in cm³/mol).</param>
/// <param name="AtomContributions">Per-atom classification and contribution breakdown.</param>
/// <param name="MethodInfo">Scientific provenance and method metadata.</param>
public sealed record CrippenResult(
    double CalculatedLogP,
    double CalculatedMr,
    IReadOnlyList<CrippenAtomContribution> AtomContributions,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// Wildman-Crippen atom contribution record.
/// </summary>
public sealed record CrippenAtomContribution(
    int AtomIndex,
    string Symbol,
    string AtomType,
    double LogPContribution,
    double MrContribution
);

/// <summary>
/// Crippen-Inspired Empirical LogP &amp; Molar Refractivity Calculator based on atom-type additive contributions.
/// Reference: Wildman, S. A., &amp; Crippen, G. M. (1999). Prediction of Physicochemical Parameters 
/// by Atomic Contributions. Journal of Chemical Information and Computer Sciences, 39(5), 868-873.
/// </summary>
public static class WildmanCrippenLogP
{
    private static readonly ScientificMethodInfo CrippenMethodInfo = new(
        "Crippen-Inspired Empirical LogP/MR (Core Fragment Subset)",
        "1999.1",
        EvidenceLevel.EmpiricalModel,
        "Organic small molecules composed of C, H, N, O, P, S, F, Cl, Br, I.",
        [
            "Additive 2D atomic property model over core hybridization environments.",
            "Coarse classification subset; unparameterized heteroatom environments use neutral zero default."
        ]
    );

    /// <summary>
    /// Computes Wildman-Crippen LogP and Molar Refractivity (MR) for any molecule.
    /// </summary>
    public static CrippenResult Calculate(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        if (!molecule.HasBondedTopology)
        {
            throw new InvalidOperationException(
                $"Molecule '{molecule.Name}' has no bonded topology. Crippen LogP/MR calculation requires a bonded molecular graph (e.g. from SMILES or Molfile/SDF), not an empirical formula without connectivity.");
        }

        double totalLogP = 0.0;
        double totalMr = 0.0;
        var contributions = new List<CrippenAtomContribution>(molecule.Atoms.Count);

        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var (type, logP, mr) = ClassifyAtom(molecule, i);
            totalLogP += logP;
            totalMr += mr;
            contributions.Add(new CrippenAtomContribution(i, molecule.Atoms[i].Element.Symbol, type, logP, mr));
        }

        return new CrippenResult(
            Math.Round(totalLogP, 3),
            Math.Round(totalMr, 3),
            contributions,
            CrippenMethodInfo
        );
    }

    private static (string Type, double LogP, double Mr) ClassifyAtom(Molecule molecule, int i)
    {
        var atom = molecule.Atoms[i];
        string sym = atom.Element.Symbol;
        var neighbors = GetNeighbors(molecule, i);

        int hCount = neighbors.Count(idx => molecule.Atoms[idx].Element.Symbol == "H");
        int cCount = neighbors.Count(idx => molecule.Atoms[idx].Element.Symbol == "C");
        int heteroCount = neighbors.Count(idx => molecule.Atoms[idx].Element.Symbol is not "C" and not "H");
        int doubleBonds = molecule.Bonds.Count(b => b.Connects(i) && b.Type == BondType.Double);
        int tripleBonds = molecule.Bonds.Count(b => b.Connects(i) && b.Type == BondType.Triple);
        int aromaticBonds = molecule.Bonds.Count(b => b.Connects(i) && b.Type == BondType.Aromatic);
        bool isAromatic = aromaticBonds > 0;

        switch (sym)
        {
            case "C":
                if (isAromatic)
                {
                    // Aromatic Carbons (C13 - C25)
                    bool hasH = hCount > 0;
                    if (hasH) return ("C18 [Aromatic C-H]", 0.1582, 3.379);
                    if (heteroCount > 0) return ("C19 [Aromatic C-Hetero]", 0.2946, 3.421);
                    return ("C20 [Aromatic C-C bridgehead / substituted]", 0.2946, 3.421);
                }
                if (tripleBonds >= 1)
                {
                    // sp Alkyne / Nitrile Carbons
                    bool isNitrile = neighbors.Any(idx => molecule.Atoms[idx].Element.Symbol == "N");
                    if (isNitrile) return ("C26 [sp Cyano C]", -0.0072, 3.447);
                    return ("C27 [sp Alkyne C]", 0.1894, 3.328);
                }
                if (doubleBonds >= 1)
                {
                    // sp2 Alkene / Carbonyl Carbons
                    bool isCarbonyl = neighbors.Any(idx => molecule.Atoms[idx].Element.Symbol == "O");
                    if (isCarbonyl) return ("C14 [sp2 Carbonyl C=O]", 0.0816, 3.011);
                    if (heteroCount >= 1) return ("C15 [sp2 C=X hetero]", 0.1250, 3.125);
                    return ("C16 [sp2 Alkene C=C]", 0.2640, 3.652);
                }
                // Aliphatic sp3 Carbons (C1 - C12)
                if (heteroCount >= 2) return ("C5 [sp3 C with >=2 heteroatoms]", -0.2050, 2.503);
                if (heteroCount == 1) return ("C6 [sp3 C with 1 heteroatom]", -0.2035, 2.753);
                return hCount switch
                {
                    >= 3 => ("C1 [sp3 Primary methyl -CH3]", 0.1441, 2.503),
                    2 => ("C2 [sp3 Secondary methylene -CH2-]", 0.1441, 2.503),
                    1 => ("C3 [sp3 Tertiary methine >CH-]", 0.0000, 2.433),
                    _ => ("C4 [sp3 Quaternary >C<]", -0.2050, 2.503)
                };

            case "H":
                // Hydrogens (H1 - H4)
                if (neighbors.Count > 0)
                {
                    var parent = molecule.Atoms[neighbors[0]];
                    if (parent.Element.Symbol is "O" or "N" or "S")
                    {
                        return ("H2 [Polar Heteroatom H (-OH, -NH, -SH)]", -0.0005, 0.922);
                    }
                    if (parent.Element.Symbol == "C")
                    {
                        bool parentAromatic = molecule.Bonds.Any(b => b.Connects(neighbors[0]) && b.Type == BondType.Aromatic);
                        if (parentAromatic) return ("H3 [Aromatic C-H]", 0.1130, 1.057);
                        return ("H1 [Aliphatic C-H]", 0.1130, 1.057);
                    }
                }
                return ("H1 [Hydrogen]", 0.1130, 1.057);

            case "N":
                // Nitrogens (N1 - N14)
                if (isAromatic)
                {
                    if (hCount >= 1) return ("N11 [Aromatic pyrrolic :NH:]", -0.5262, 3.776);
                    return ("N12 [Aromatic pyridyl :N:]", -0.4800, 3.509);
                }
                if (tripleBonds >= 1) return ("N14 [Nitrile #N]", -0.4900, 3.000);
                if (doubleBonds >= 1) return ("N13 [Imine =N-]", -0.3200, 3.200);

                // Check for amide
                bool isAmide = neighbors.Any(n => molecule.Atoms[n].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b => b.Connects(n) && b.Type == BondType.Double &&
                        molecule.Atoms[b.Atom1Index == n ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O"));

                if (isAmide) return ("N6 [Amide nitrogen]", -0.4700, 3.760);
                return hCount switch
                {
                    >= 2 => ("N1 [Primary aliphatic amine -NH2]", -0.6700, 3.224),
                    1 => ("N2 [Secondary aliphatic amine -NH-]", -0.5800, 3.010),
                    _ => ("N3 [Tertiary aliphatic amine -N<]", -0.4200, 2.850)
                };

            case "O":
                // Oxygens (O1 - O12)
                if (doubleBonds >= 1)
                {
                    return ("O2 [Carbonyl / Nitro =O]", -0.3339, 1.666);
                }
                if (hCount >= 1)
                {
                    bool attachedToAromatic = neighbors.Any(n => molecule.Bonds.Any(b => b.Connects(n) && b.Type == BondType.Aromatic));
                    if (attachedToAromatic) return ("O4 [Phenolic -OH]", -0.0341, 1.802);
                    return ("O1 [Aliphatic -OH]", -0.0475, 1.701);
                }
                return ("O3 [Ether / Ester bridging -O-]", 0.0817, 1.603);

            case "F":
                return ("F [Fluorine]", 0.4202, 1.138);

            case "Cl":
                return ("Cl [Chlorine]", 0.6895, 5.853);

            case "Br":
                return ("Br [Bromine]", 0.8456, 8.927);

            case "I":
                return ("I [Iodine]", 1.1428, 13.948);

            case "S":
                if (doubleBonds >= 1) return ("S2 [Sulfoxide/Thiocarbonyl =S]", -0.1500, 7.500);
                if (hCount >= 1) return ("S1 [Thiol -SH]", 0.6482, 7.315);
                return ("S3 [Thioether -S-]", 0.5400, 7.120);

            case "P":
                return ("P [Phosphorus]", 0.0800, 8.200);

            default:
                return ($"{sym} [Default atom]", 0.0, 0.0);
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
}
