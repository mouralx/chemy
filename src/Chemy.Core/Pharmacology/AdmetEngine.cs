using Chemy.Core.Structure;

namespace Chemy.Core.Pharmacology;

/// <summary>
/// Comprehensive, industrial-grade ADMET &amp; drug-likeness profile.
/// </summary>
/// <param name="Formula">Empirical chemical formula.</param>
/// <param name="MolecularWeight">Molecular mass in g/mol (Pfizer Rule limit: &lt;= 500).</param>
/// <param name="CalculatedLogP">Calculated Wildman-Crippen partition coefficient (Pfizer Rule limit: &lt;= 5.0).</param>
/// <param name="TpsaAngstrom2">Ertl Topological Polar Surface Area in Å² (Veber limit: &lt;= 140 Å²).</param>
/// <param name="HydrogenBondDonors">Number of hydrogen bond donor groups (-OH, -NH).</param>
/// <param name="HydrogenBondAcceptors">Number of hydrogen bond acceptor atoms (N, O, F).</param>
/// <param name="RotatableBonds">Number of non-terminal, single rotatable bonds (Veber limit: &lt;= 10).</param>
/// <param name="AromaticRings">Count of aromatic ring systems.</param>
/// <param name="LipinskiViolations">Total number of Lipinski Rule of 5 criteria violated (0 to 4).</param>
/// <param name="PassesLipinskiRuleOf5">True if 0 or 1 Lipinski criteria are violated.</param>
/// <param name="PassesVeberRules">True if RotatableBonds &lt;= 10 and TPSA &lt;= 140 Å².</param>
/// <param name="PassesGhoseFilter">True if 160 &lt;= MW &lt;= 480 and -0.4 &lt;= LogP &lt;= 5.6.</param>
/// <param name="QedDrugLikenessScore">Quantitative Estimate of Drug-Likeness (0.0 to 1.0).</param>
/// <param name="HergCardiacRisk">Estimated hERG potassium channel cardiotoxicity risk classification.</param>
/// <param name="Cyp450MetabolismSite">Predicted Phase-I CYP450 hepatic oxidation cleavage site.</param>
/// <param name="BloodBrainBarrierPermeability">Predicted Central Nervous System (CNS) blood-brain barrier permeability.</param>
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
    string HergCardiacRisk,
    string Cyp450MetabolismSite,
    string BloodBrainBarrierPermeability
);

/// <summary>
/// Industrial-Grade ADMET &amp; Chemoinformatics Property Calculator.
/// Implements standard Ertl Topological Polar Surface Area (TPSA), Wildman-Crippen atom-typing LogP,
/// Lipinski Rule of 5, Veber Oral Bioavailability rules, and Ghose drug-likeness filters.
/// </summary>
public static class AdmetEngine
{
    /// <summary>
    /// Computes a comprehensive ADMET profile for any given molecule.
    /// </summary>
    public static AdmetProfile Analyze(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

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

        // Cardiac hERG risk prediction (Lipophilic bases with high LogP and low TPSA)
        string hergRisk;
        if (logP > 3.8 && tpsa < 60.0 && molecule.Atoms.Any(a => a.Element.Symbol == "N"))
        {
            hergRisk = "High Alert (High LogP + Basic Nitrogen -> Potential QT Prolongation Risk)";
        }
        else if (logP > 2.5)
        {
            hergRisk = "Moderate Risk (Monitor hERG patch clamp in vitro)";
        }
        else
        {
            hergRisk = "Low Risk (Normal cardiac safety window)";
        }

        // Phase-I CYP450 metabolism prediction
        string cypSite;
        var fgs = molecule.GetFunctionalGroups().Select(f => f.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (fgs.Contains("Ester"))
        {
            cypSite = "Carboxylesterase / CYP3A4: Rapid ester hydrolysis";
        }
        else if (fgs.Contains("Amine"))
        {
            cypSite = "CYP2D6 / CYP3A4: Oxidative N-dealkylation";
        }
        else if (fgs.Contains("Aromatic"))
        {
            cypSite = "CYP1A2 / CYP2C9: Aromatic para-hydroxylation";
        }
        else if (fgs.Contains("Alcohol"))
        {
            cypSite = "Alcohol Dehydrogenase / UGT: Phase-II Glucuronidation";
        }
        else
        {
            cypSite = "CYP450 Omega-1 Aliphatic Hydroxylation";
        }

        // Blood-Brain Barrier (BBB) permeability
        string bbb = (tpsa < 90.0 && logP is >= 1.0 and <= 3.5 && mw < 400.0)
            ? "High BBB Permeability (CNS Active)"
            : "Low BBB Permeability (Peripherally Restricted)";

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
            hergRisk,
            cypSite,
            bbb
        );
    }

    /// <summary>
    /// Calculates Topological Polar Surface Area (TPSA) using standard Ertl atomic fragment contributions.
    /// Reference: Ertl et al., J. Med. Chem. 2000, 43, 3714-3717.
    /// </summary>
    public static double CalculateErtlTpsa(Molecule molecule)
    {
        return ErtlTpsa.Calculate(molecule).TotalTpsa;
    }

    /// <summary>
    /// Computes Wildman-Crippen partition coefficient (LogP).
    /// Reference: Wildman &amp; Crippen, J. Chem. Inf. Comput. Sci. 1999, 39, 868-873.
    /// </summary>
    public static double CalculateCrippenLogP(Molecule molecule)
    {
        return WildmanCrippenLogP.Calculate(molecule).CalculatedLogP;
    }

    private static List<int> GetNeighborIndices(Molecule molecule, int atomIndex) =>
        molecule.Bonds
            .Where(b => b.Connects(atomIndex))
            .Select(b => b.Atom1Index == atomIndex ? b.Atom2Index : b.Atom1Index)
            .ToList();

    private static int CountHydrogenBondDonors(Molecule molecule)
    {
        int count = 0;
        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var atom = molecule.Atoms[i];
            if (atom.Element.Symbol is "O" or "N")
            {
                // Check if explicitly connected to Hydrogen
                bool hasExplicitH = molecule.Bonds.Any(b =>
                {
                    if (!b.Connects(i)) return false;
                    int other = b.Atom1Index == i ? b.Atom2Index : b.Atom1Index;
                    return molecule.Atoms[other].Element.Symbol == "H";
                });

                if (hasExplicitH)
                {
                    count++;
                }
                else
                {
                    bool isAromatic = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Aromatic);
                    if (!isAromatic)
                    {
                        // Check implicit hydrogens from standard aliphatic valence
                        int bondValence = molecule.Bonds.Where(b => b.Connects(i)).Sum(b => b.Type switch
                        {
                            BondType.Double => 2,
                            BondType.Triple => 3,
                            _ => 1
                        });

                        if (atom.Element.Symbol == "O" && bondValence < 2) count++;
                        else if (atom.Element.Symbol == "N" && bondValence < 3) count++;
                    }
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
            var sym = molecule.Atoms[i].Element.Symbol;
            if (sym == "O")
            {
                count++;
            }
            else if (sym == "N")
            {
                bool isPyrrolic = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Aromatic) &&
                    molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H");
                if (!isPyrrolic) count++;
            }
        }
        return count;
    }

    private static int CountRotatableBonds(Molecule molecule)
    {
        int rotatable = 0;
        foreach (var bond in molecule.Bonds)
        {
            if (bond.Type == BondType.Single)
            {
                var e1 = molecule.Atoms[bond.Atom1Index].Element.Symbol;
                var e2 = molecule.Atoms[bond.Atom2Index].Element.Symbol;

                // Non-terminal bonds (neither atom is H or Halogen) and NOT in a cyclic ring
                if (e1 != "H" && e2 != "H" && e1 != "F" && e2 != "F" && e1 != "Cl" && e2 != "Cl" && e1 != "Br" && e2 != "Br")
                {
                    if (!IsBondInRing(molecule, bond))
                    {
                        int heavyDeg1 = molecule.Bonds.Count(b => b.Connects(bond.Atom1Index) && molecule.Atoms[b.Atom1Index == bond.Atom1Index ? b.Atom2Index : b.Atom1Index].Element.Symbol != "H");
                        int heavyDeg2 = molecule.Bonds.Count(b => b.Connects(bond.Atom2Index) && molecule.Atoms[b.Atom1Index == bond.Atom2Index ? b.Atom2Index : b.Atom1Index].Element.Symbol != "H");
                        if (heavyDeg1 > 1 && heavyDeg2 > 1) rotatable++;
                    }
                }
            }
        }
        return Math.Max(0, rotatable);
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
                if (neighbor == target) return true;
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
        var graph = Graph.ChemicalGraph.FromMolecule(molecule);
        var rings = graph.FindRings();
        int count = 0;
        foreach (var ring in rings)
        {
            bool hasAromatic = ring.Any(nodeId => graph.GetIncidentEdges(nodeId).Any(e => e.IsAromatic || e.BondType == BondType.Aromatic));
            if (hasAromatic && ring.Count is 5 or 6)
            {
                count++;
            }
        }
        return Math.Max(count, molecule.Bonds.Any(b => b.Type == BondType.Aromatic) ? 1 : 0);
    }

    private static int CountStructuralAlerts(Molecule molecule)
    {
        // Counts reactive toxicophores (PAINS / Brenk filter alerts)
        int alerts = 0;
        var fgs = molecule.GetFunctionalGroups();

        // 1. Reactive Halides
        bool hasAlkylHalide = molecule.Bonds.Any(b =>
        {
            string e1 = molecule.Atoms[b.Atom1Index].Element.Symbol;
            string e2 = molecule.Atoms[b.Atom2Index].Element.Symbol;
            return (e1 == "C" && e2 is "Cl" or "Br" or "I") || (e2 == "C" && e1 is "Cl" or "Br" or "I");
        });
        if (hasAlkylHalide && !fgs.Contains(FunctionalGroup.Aromatic)) alerts++;

        // 2. Nitro groups
        if (fgs.Contains(FunctionalGroup.Nitro)) alerts++;

        // 3. Reactive aldehydes
        if (fgs.Contains(FunctionalGroup.Aldehyde)) alerts++;

        return alerts;
    }

    private static double CalculateQedScore(Molecule molecule) =>
        BickertonQed.Calculate(molecule).QedScore;
}
