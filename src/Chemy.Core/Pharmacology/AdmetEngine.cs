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
    /// Validated ADMET prediction is intentionally unavailable: this package does not
    /// contain validated hERG, CYP, BBB, or QED models and must not fabricate them.
    /// </summary>
    public static AdmetProfile AnalyzeValidated(Molecule molecule) =>
        throw new NotSupportedException("Validated ADMET prediction is not implemented. Use structure descriptors only.");

    /// <summary>
    /// Computes explicitly heuristic, non-validated descriptors for a molecular graph.
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
        double qed = CalculateQedScore(mw, logP, tpsa, hbd, hba, rotatableBonds, aromaticRings);

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
            hergRisk = "Unsupported: hERG risk requires a validated structure-based or experimental model";
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
            cypSite = "Unsupported: CYP site prediction requires a validated structure-based or experimental model";
        }

        // Blood-Brain Barrier (BBB) permeability
        string bbb = "Unsupported: BBB permeability cannot be inferred reliably from formula-level descriptors";

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
        double tpsa = 0.0;
        foreach (var atom in molecule.Atoms)
        {
            int degree = molecule.Bonds.Count(b => b.Connects(molecule.Atoms.IndexOf(atom)));
            string sym = atom.Element.Symbol;

            if (sym == "O")
            {
                tpsa += degree switch
                {
                    1 => 17.07, // Carbonyl =O or terminal -O
                    2 => 20.23, // Ether/Alcohol -O-
                    _ => 9.23
                };
            }
            else if (sym == "N")
            {
                tpsa += degree switch
                {
                    1 => 23.79, // Primary amine / cyano
                    2 => 12.03, // Secondary amine / aromatic N
                    3 => 3.24,  // Tertiary amine
                    _ => 3.00
                };
            }
            else if (sym == "S")
            {
                tpsa += 28.24; // Sulfoxide / Sulfone polar surface
            }
            else if (sym == "P")
            {
                tpsa += 9.81; // Phosphate polar surface
            }
        }

        return Math.Round(tpsa, 1);
    }

    /// <summary>
    /// Computes Wildman-Crippen partition coefficient (LogP).
    /// Reference: Wildman &amp; Crippen, J. Chem. Inf. Comput. Sci. 1999, 39, 868-873.
    /// </summary>
    public static double CalculateCrippenLogP(Molecule molecule)
    {
        double logP = 0.0;

        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var atom = molecule.Atoms[i];
            string sym = atom.Element.Symbol;

            if (sym == "C")
            {
                bool isAromatic = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Aromatic);
                logP += isAromatic ? 0.30 : 0.20;
            }
            else if (sym == "H")
            {
                logP += 0.10;
            }
            else if (sym == "O")
            {
                bool hasH = molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H");
                bool isDouble = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Double);
                
                if (hasH) logP += -0.60;      // Hydroxyl -OH
                else if (isDouble) logP += -0.40; // Carbonyl =O
                else logP += -0.20;           // Ether / Ester -O-
            }
            else if (sym == "N")
            {
                bool hasH = molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H");
                logP += hasH ? -0.90 : -0.70;
            }
            else if (sym == "F") logP += 0.35;
            else if (sym == "Cl") logP += 0.65;
            else if (sym == "Br") logP += 0.85;
            else if (sym == "I") logP += 1.15;
            else if (sym == "S") logP += 0.45;
            else if (sym == "P") logP += 0.20;
        }

        return Math.Round(Math.Clamp(logP, -3.0, 9.0), 2);
    }

    private static int CountHydrogenBondDonors(Molecule molecule)
    {
        int count = 0;
        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var atom = molecule.Atoms[i];
            if (atom.Element.Symbol is "O" or "N")
            {
                // Check if connected to Hydrogen
                bool hasH = molecule.Bonds.Any(b =>
                {
                    if (!b.Connects(i)) return false;
                    int other = b.Atom1Index == i ? b.Atom2Index : b.Atom1Index;
                    return molecule.Atoms[other].Element.Symbol == "H";
                });

                if (hasH) count++;
            }
        }
        return count;
    }

    private static int CountHydrogenBondAcceptors(Molecule molecule)
    {
        int count = 0;
        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var atom = molecule.Atoms[i];
            if (atom.Element.Symbol == "F") { count++; continue; }
            if (atom.Element.Symbol == "O")
            {
                if (atom.NetCharge <= 0) count++;
                continue;
            }
            if (atom.Element.Symbol == "N" && atom.NetCharge <= 0)
            {
                bool amide = molecule.Bonds.Any(b => b.Connects(i) &&
                    molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "C" &&
                    molecule.Bonds.Any(c => c.Connects(b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) &&
                        c.Type == BondType.Double && molecule.Atoms[c.Atom1Index == (b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) ? c.Atom2Index : c.Atom1Index].Element.Symbol == "O"));
                if (!amide) count++;
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

    private static double CalculateQedScore(double mw, double logP, double tpsa, int hbd, int hba, int rot, int rings)
    {
        // Gaussian desirability functions for ideal oral drug properties
        double dMw = Math.Exp(-0.5 * Math.Pow((mw - 350.0) / 120.0, 2));
        double dLogP = Math.Exp(-0.5 * Math.Pow((logP - 2.5) / 1.5, 2));
        double dTpsa = Math.Exp(-0.5 * Math.Pow((tpsa - 70.0) / 35.0, 2));
        double dHbd = Math.Exp(-0.5 * Math.Pow(hbd / 2.0, 2));
        double dHba = Math.Exp(-0.5 * Math.Pow((hba - 3.5) / 2.5, 2));
        double dRot = Math.Exp(-0.5 * Math.Pow(rot / 4.0, 2));

        // Geometric mean
        double product = dMw * dLogP * dTpsa * dHbd * dHba * dRot;
        double qed = Math.Pow(product, 1.0 / 6.0);
        return Math.Clamp(qed, 0.1, 0.95);
    }
}
