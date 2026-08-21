namespace Chemy.Core.Pharmacology;

using Chemy.Core.Graph;
using Chemy.Core.Scientific;
using Chemy.Core.Structure;

/// <summary>
/// Chemoinformatics Physicochemical &amp; Drug-Likeness Profile.
/// Encapsulates calculated molecular descriptors (LogP, TPSA, QED, MW) and standard rule-based medicinal chemistry filters.
/// </summary>
/// <param name="Formula">Hill-system chemical formula.</param>
/// <param name="MolecularWeight">Molecular mass in g/mol (Pfizer Rule limit: &lt;= 500).</param>
/// <param name="CalculatedLogP">Calculated Crippen partition coefficient (Pfizer Rule limit: &lt;= 5.0).</param>
/// <param name="TpsaAngstrom2">Topological Polar Surface Area in Å² (Veber limit: &lt;= 140 Å²).</param>
/// <param name="HydrogenBondDonors">Number of hydrogen bond donor groups (-OH, -NH).</param>
/// <param name="HydrogenBondAcceptors">Number of hydrogen bond acceptor atoms (N, O, F).</param>
/// <param name="RotatableBonds">Number of non-terminal, single rotatable bonds (Veber limit: &lt;= 10).</param>
/// <param name="AromaticRings">Count of aromatic ring systems from SSSR minimum cycle basis.</param>
/// <param name="LipinskiViolations">Total number of Lipinski Rule of 5 criteria violated (0 to 4).</param>
/// <param name="PassesLipinskiRuleOf5">True if 0 or 1 Lipinski criteria are violated.</param>
/// <param name="PassesVeberRules">True if RotatableBonds &lt;= 10 and TPSA &lt;= 140 Å².</param>
/// <param name="PassesGhoseFilter">True if 160 &lt;= MW &lt;= 480 and -0.4 &lt;= LogP &lt;= 5.6.</param>
/// <param name="QedDrugLikenessScore">Quantitative Estimate of Drug-Likeness desirability score (0.0 to 1.0).</param>
/// <param name="MethodInfo">Scientific method provenance and metadata.</param>
public record AdmetProfile(
    string Formula,
    double MolecularWeight,
    double CalculatedLogP,
    double TpsaAngstrom2,
    int HydrogenBondDonors,
    int HydrogenBondAcceptors,
    int RotatableBonds,
    int AromaticRings,
    int LipinskiViolations,
    bool PassesLipinskiRuleOf5,
    bool PassesVeberRules,
    bool PassesGhoseFilter,
    double QedDrugLikenessScore,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// Chemoinformatics &amp; Drug-Likeness Property Calculator.
/// Computes Topological Polar Surface Area (TPSA), Crippen atom-typing LogP,
/// Lipinski Rule of 5, Veber Oral Bioavailability rules, and Ghose drug-likeness filters.
/// </summary>
public static class AdmetEngine
{
    private static readonly ScientificMethodInfo AdmetMethodInfo = new(
        "Chemoinformatics Physicochemical & Drug-Likeness Filter Suite",
        "2026.1",
        EvidenceLevel.EmpiricalModel,
        "Neutral or singly charged organic molecules with standard covalent topology.",
        [
            "Rule-based physicochemical filters (Lipinski, Veber, Ghose) and empirical descriptors.",
            "Does NOT assess in vitro/in vivo biological safety, pharmacokinetics, hERG cardiotoxicity, or clinical outcomes."
        ]
    );

    /// <summary>
    /// Computes a comprehensive physicochemical and drug-likeness profile for a bonded molecule.
    /// </summary>
    public static AdmetProfile Analyze(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        if (!molecule.HasBondedTopology)
        {
            throw new InvalidOperationException(
                $"Molecule '{molecule.Name}' has no bonded topology. Chemoinformatics descriptors (TPSA, LogP, QED, Lipinski) require a bonded molecular graph (e.g. from SMILES or Molfile/SDF), not an empirical formula without connectivity.");
        }

        double mw = molecule.MolecularWeight;
        double logP = CalculateCrippenLogP(molecule);
        double tpsa = CalculateErtlTpsa(molecule);
        int hbd = CountHydrogenBondDonors(molecule);
        int hba = CountHydrogenBondAcceptors(molecule);
        int rotatableBonds = CountRotatableBonds(molecule);
        int aromaticRings = CountAromaticRings(molecule);

        // Lipinski Rule of 5 (Pfizer criteria)
        int violations = 0;
        if (mw > 500.0) violations++;
        if (logP > 5.0) violations++;
        if (hbd > 5) violations++;
        if (hba > 10) violations++;

        bool passesLipinski = violations <= 1;
        bool passesVeber = rotatableBonds <= 10 && tpsa <= 140.0;
        bool passesGhose = mw >= 160.0 && mw <= 480.0 && logP >= -0.4 && logP <= 5.6;

        // Quantitative Estimate of Drug-Likeness (QED) Score (0.0 to 1.0)
        double qed = CalculateQedScore(molecule);

        return new AdmetProfile(
            molecule.ChemicalFormula,
            Math.Round(mw, 2),
            Math.Round(logP, 2),
            Math.Round(tpsa, 1),
            hbd,
            hba,
            rotatableBonds,
            aromaticRings,
            violations,
            passesLipinski,
            passesVeber,
            passesGhose,
            Math.Round(qed, 3),
            AdmetMethodInfo
        );
    }

    /// <summary>
    /// Calculates Topological Polar Surface Area (TPSA) using standard Ertl atomic fragment contributions.
    /// </summary>
    public static double CalculateErtlTpsa(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);
        return ErtlTpsa.Calculate(molecule).TotalTpsa;
    }

    /// <summary>
    /// Calculates Wildman-Crippen logP using atom-type contribution summing.
    /// </summary>
    public static double CalculateCrippenLogP(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);
        return WildmanCrippenLogP.Calculate(molecule).CalculatedLogP;
    }

    /// <summary>
    /// Calculates Bickerton Quantitative Estimate of Drug-likeness (QED).
    /// </summary>
    public static double CalculateQedScore(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);
        return BickertonQed.Calculate(molecule).QedScore;
    }

    private static int CountHydrogenBondDonors(Molecule molecule)
    {
        int count = 0;
        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var a = molecule.Atoms[i];
            if (a.Element.Symbol is "O" or "N")
            {
                int hNeighbors = molecule.Bonds
                    .Where(b => b.Connects(i))
                    .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                    .Count(neighborIdx => molecule.Atoms[neighborIdx].Element.Symbol == "H");

                if (hNeighbors > 0)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private static int CountHydrogenBondAcceptors(Molecule molecule)
    {
        int count = 0;
        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var a = molecule.Atoms[i];
            if (a.NetCharge > 0) continue;

            if (a.Element.Symbol == "O")
            {
                // Exclude carboxylic acid -OH oxygens where the lone pair is resonance-delocalized into carbonyl C=O
                bool hasHydrogen = molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H");
                bool isCarboxylicHydroxyl = hasHydrogen && molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b2 => b2.Connects(b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) && b2.Type == BondType.Double &&
                        molecule.Atoms[b2.Atom1Index == (b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) ? b2.Atom2Index : b2.Atom1Index].Element.Symbol == "O"));

                if (!isCarboxylicHydroxyl)
                {
                    count++;
                }
            }
            else if (a.Element.Symbol == "N")
            {
                // Exclude amide nitrogens (resonance delocalized with carbonyl)
                bool isAmide = molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b2 => b2.Connects(b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) && b2.Type == BondType.Double &&
                        molecule.Atoms[b2.Atom1Index == (b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) ? b2.Atom2Index : b2.Atom1Index].Element.Symbol == "O"));

                // Exclude nitro group nitrogens (-NO2)
                int oxygenNeighbors = molecule.Bonds.Count(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O");
                bool isNitro = oxygenNeighbors >= 2;

                if (!isAmide && !isNitro)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private static int CountRotatableBonds(Molecule molecule)
    {
        int rotatable = 0;

        foreach (var bond in molecule.Bonds)
        {
            if (bond.Type != BondType.Single) continue;

            var a1 = molecule.Atoms[bond.Atom1Index];
            var a2 = molecule.Atoms[bond.Atom2Index];

            if (a1.Element.Symbol == "H" || a2.Element.Symbol == "H") continue;

            int heavyDeg1 = molecule.Bonds.Count(b => b.Connects(bond.Atom1Index) && molecule.Atoms[b.Atom1Index == bond.Atom1Index ? b.Atom2Index : b.Atom1Index].Element.Symbol != "H");
            int heavyDeg2 = molecule.Bonds.Count(b => b.Connects(bond.Atom2Index) && molecule.Atoms[b.Atom1Index == bond.Atom2Index ? b.Atom2Index : b.Atom1Index].Element.Symbol != "H");

            if (heavyDeg1 <= 1 || heavyDeg2 <= 1) continue;

            if (IsBondInRing(molecule, bond)) continue;

            // Exclude amide C-N and ester C-O single bonds (partial double-bond resonance character)
            int cIdx = a1.Element.Symbol == "C" && a2.Element.Symbol is "N" or "O" ? bond.Atom1Index : (a2.Element.Symbol == "C" && a1.Element.Symbol is "N" or "O" ? bond.Atom2Index : -1);
            if (cIdx >= 0)
            {
                bool isCarbonylCarbon = molecule.Bonds.Any(b => b.Connects(cIdx) && b.Type == BondType.Double &&
                    molecule.Atoms[b.Atom1Index == cIdx ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O");
                if (isCarbonylCarbon) continue;
            }

            rotatable++;
        }

        return rotatable;
    }

    private static bool IsBondInRing(Molecule molecule, Bond bond)
    {
        int start = bond.Atom1Index;
        int target = bond.Atom2Index;

        var visited = new HashSet<int> { start };
        var queue = new Queue<int>();

        foreach (var b in molecule.Bonds)
        {
            if (b == bond) continue;
            if (b.Connects(start))
            {
                int neighbor = b.Atom1Index == start ? b.Atom2Index : b.Atom1Index;
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == target) return true;

            foreach (var b in molecule.Bonds)
            {
                if (b == bond) continue;
                if (b.Connects(current))
                {
                    int neighbor = b.Atom1Index == current ? b.Atom2Index : b.Atom1Index;
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return false;
    }

    private static int CountAromaticRings(Molecule molecule)
    {
        var sssr = CycleBasis.ComputeSssr(molecule);
        int count = 0;
        foreach (var ring in sssr.Rings)
        {
            if (ring.Count is 5 or 6 && ring.All(atomIdx => molecule.Bonds.Any(b => b.Connects(atomIdx) && b.Type == BondType.Aromatic)))
            {
                count++;
            }
        }
        return Math.Max(count, molecule.Bonds.Any(b => b.Type == BondType.Aromatic) ? 1 : 0);
    }
}
