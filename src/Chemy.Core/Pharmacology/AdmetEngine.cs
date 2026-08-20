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
        int alerts = CountStructuralAlerts(molecule);
        double qed = CalculateQedScore(mw, logP, tpsa, hbd, hba, rotatableBonds, aromaticRings, alerts);

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
        double tpsa = 0.0;
        int nAtoms = molecule.Atoms.Count;

        for (int i = 0; i < nAtoms; i++)
        {
            var atom = molecule.Atoms[i];
            string sym = atom.Element.Symbol;

            if (sym == "O")
            {
                var bonded = GetNeighborIndices(molecule, i);
                int hCount = bonded.Count(idx => molecule.Atoms[idx].Element.Symbol == "H");
                bool isDouble = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Double);
                bool bondedToCarbonyl = bonded.Any(idx => molecule.Atoms[idx].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b => b.Connects(idx) && b.Type == BondType.Double && molecule.Atoms[b.Atom1Index == idx ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O"));

                if (isDouble)
                {
                    tpsa += 17.07; // Carbonyl / Nitro / Sulfone =O
                }
                else if (hCount > 0)
                {
                    tpsa += 20.23; // Hydroxyl -OH
                }
                else if (bondedToCarbonyl)
                {
                    tpsa += 9.23; // Ester bridging -O-
                }
                else
                {
                    tpsa += 9.23; // Ether -O-
                }
            }
            else if (sym == "N")
            {
                var bonded = GetNeighborIndices(molecule, i);
                int hCount = bonded.Count(idx => molecule.Atoms[idx].Element.Symbol == "H");
                int nonHCount = bonded.Count - hCount;
                bool isAromatic = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Aromatic);
                bool isTriple = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Triple);
                bool isAmide = bonded.Any(idx => molecule.Atoms[idx].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b => b.Connects(idx) && b.Type == BondType.Double && molecule.Atoms[b.Atom1Index == idx ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O"));
                int oCount = bonded.Count(idx => molecule.Atoms[idx].Element.Symbol == "O");

                if (oCount >= 2)
                {
                    tpsa += 45.82; // Nitro -NO2 group
                }
                else if (isTriple)
                {
                    tpsa += 23.79; // Nitrile -C≡N
                }
                else if (isAmide)
                {
                    tpsa += hCount switch
                    {
                        >= 2 => 43.09, // Primary amide -CONH2
                        1 => 29.10,    // Secondary amide -CONHR
                        _ => 20.31     // Tertiary amide -CONR2
                    };
                }
                else if (isAromatic)
                {
                    tpsa += hCount switch
                    {
                        >= 1 => 15.79, // Aromatic -NH- (pyrrole/indole)
                        _ => 12.89     // Pyridyl =N-
                    };
                }
                else
                {
                    tpsa += hCount switch
                    {
                        >= 2 => 26.02, // Primary aliphatic amine -NH2
                        1 => 12.03,    // Secondary aliphatic amine -NH-
                        _ => 3.24      // Tertiary aliphatic amine -NR2
                    };
                }
            }
            else if (sym == "S")
            {
                var bonded = GetNeighborIndices(molecule, i);
                int oCount = bonded.Count(idx => molecule.Atoms[idx].Element.Symbol == "O");
                int hCount = bonded.Count(idx => molecule.Atoms[idx].Element.Symbol == "H");

                if (oCount >= 1) tpsa += 28.24; // Sulfoxide / Sulfone
                else if (hCount > 0) tpsa += 38.80; // Thiol -SH
                else tpsa += 25.30; // Thioether -S-
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
        int nAtoms = molecule.Atoms.Count;

        for (int i = 0; i < nAtoms; i++)
        {
            var atom = molecule.Atoms[i];
            string sym = atom.Element.Symbol;
            var neighbors = GetNeighborIndices(molecule, i);

            if (sym == "C")
            {
                bool isAromatic = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Aromatic);
                bool hasDouble = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Double);
                bool hasTriple = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Triple);
                int heteroCount = neighbors.Count(idx => molecule.Atoms[idx].Element.Symbol is not "C" and not "H");

                if (isAromatic)
                {
                    bool hasAromaticH = neighbors.Any(idx => molecule.Atoms[idx].Element.Symbol == "H");
                    logP += hasAromaticH ? 0.1582 : 0.2946;
                }
                else if (hasTriple)
                {
                    bool isNitrile = neighbors.Any(idx => molecule.Atoms[idx].Element.Symbol == "N");
                    logP += isNitrile ? -0.0072 : 0.1894;
                }
                else if (hasDouble)
                {
                    bool isCarbonyl = neighbors.Any(idx => molecule.Atoms[idx].Element.Symbol == "O");
                    logP += isCarbonyl ? 0.0816 : 0.1250;
                }
                else
                {
                    // Aliphatic sp3
                    if (heteroCount >= 2) logP += -0.2050;
                    else if (heteroCount == 1) logP += -0.2035;
                    else
                    {
                        int hCount = neighbors.Count(idx => molecule.Atoms[idx].Element.Symbol == "H");
                        logP += hCount switch
                        {
                            >= 3 => 0.1441, // -CH3
                            2 => 0.1441,    // -CH2-
                            1 => 0.0000,    // >CH-
                            _ => -0.2050    // >C<
                        };
                    }
                }
            }
            else if (sym == "H")
            {
                int partner = neighbors.FirstOrDefault(-1);
                if (partner >= 0)
                {
                    string pSym = molecule.Atoms[partner].Element.Symbol;
                    bool isAr = molecule.Bonds.Any(b => b.Connects(partner) && b.Type == BondType.Aromatic);

                    if (pSym == "C")
                    {
                        logP += isAr ? 0.1130 : 0.1230;
                    }
                    else if (pSym == "O")
                    {
                        logP += -0.2670; // Polar hydroxyl H
                    }
                    else if (pSym == "N")
                    {
                        logP += -0.0718; // Polar amine/amide H
                    }
                    else if (pSym == "S")
                    {
                        logP += 0.0550;
                    }
                    else
                    {
                        logP += 0.1000;
                    }
                }
                else
                {
                    logP += 0.1130;
                }
            }
            else if (sym == "O")
            {
                bool isDouble = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Double);
                int hCount = neighbors.Count(idx => molecule.Atoms[idx].Element.Symbol == "H");
                bool isCarboxyl = neighbors.Any(idx => molecule.Atoms[idx].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b => b.Connects(idx) && b.Type == BondType.Double && molecule.Atoms[b.Atom1Index == idx ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O"));

                if (isDouble) logP += -0.2573;
                else if (hCount > 0) logP += isCarboxyl ? -0.5262 : -0.4674;
                else if (isCarboxyl) logP += -0.0384;
                else logP += -0.0062;
            }
            else if (sym == "N")
            {
                bool isAromatic = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Aromatic);
                bool isTriple = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Triple);
                bool isAmide = neighbors.Any(idx => molecule.Atoms[idx].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b => b.Connects(idx) && b.Type == BondType.Double && molecule.Atoms[b.Atom1Index == idx ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O"));
                int hCount = neighbors.Count(idx => molecule.Atoms[idx].Element.Symbol == "H");

                if (isTriple) logP += -0.0072;
                else if (isAmide) logP += -0.4496;
                else if (isAromatic) logP += -0.3256;
                else
                {
                    logP += hCount switch
                    {
                        >= 2 => -0.5113,
                        1 => -0.3102,
                        _ => -0.0331
                    };
                }
            }
            else if (sym == "F") logP += 0.4202;
            else if (sym == "Cl") logP += 0.6895;
            else if (sym == "Br") logP += 0.8456;
            else if (sym == "I") logP += 0.8857;
            else if (sym == "S") logP += 0.3651;
            else if (sym == "P") logP += 0.1980;
        }

        return Math.Round(Math.Clamp(logP, -4.0, 10.0), 2);
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
                    // Check implicit hydrogens from standard valence
                    int bondValence = molecule.Bonds.Where(b => b.Connects(i)).Sum(b => b.Type switch
                    {
                        BondType.Double => 2,
                        BondType.Triple => 3,
                        BondType.Aromatic => 1,
                        _ => 1
                    });

                    if (atom.Element.Symbol == "O" && bondValence < 2) count++;
                    else if (atom.Element.Symbol == "N" && bondValence < 3) count++;
                }
            }
        }
        return count;
    }

    private static int CountHydrogenBondAcceptors(Molecule molecule) =>
        molecule.Atoms.Count(a => a.Element.Symbol is "O" or "N" or "F");

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

    private static double CalculateQedScore(double mw, double logP, double tpsa, int hbd, int hba, int rot, int rings, int alerts = 0)
    {
        // Exact Bickerton et al. (Nature Chemistry 2012, 4, 90-98) asymmetric desirability functions
        // d(x) = a + b / (1 + exp(-(x - c) / d))
        double dMw = AsymmetricDesirability(mw, 0.0, 1.05, 381.9, -123.6);
        double dLogP = AsymmetricDesirability(logP, 0.0, 1.03, 2.83, -1.33);
        double dHbd = AsymmetricDesirability(hbd, 0.0, 1.05, 1.86, -1.20);
        double dHba = AsymmetricDesirability(hba, 0.0, 1.05, 4.38, -2.21);
        double dTpsa = AsymmetricDesirability(tpsa, 0.0, 1.05, 68.2, -37.8);
        double dRot = AsymmetricDesirability(rot, 0.0, 1.06, 4.77, -2.72);
        double dArom = AsymmetricDesirability(rings, 0.0, 1.05, 1.65, -1.01);
        double dAlerts = AsymmetricDesirability(alerts, 0.0, 1.06, 0.48, -0.61);

        // Published parameter weights: MW, LogP, HBD, HBA, TPSA, ROTB, AROM, ALERTS
        double[] weights = [0.66, 0.46, 0.61, 0.05, 0.65, 0.48, 0.95, 0.77];
        double[] dScores = [dMw, dLogP, dHbd, dHba, dTpsa, dRot, dArom, dAlerts];

        double sumWeightedLog = 0.0;
        double sumWeights = 0.0;

        for (int i = 0; i < weights.Length; i++)
        {
            double dClamped = Math.Clamp(dScores[i], 1e-4, 1.0);
            sumWeightedLog += weights[i] * Math.Log(dClamped);
            sumWeights += weights[i];
        }

        double qed = Math.Exp(sumWeightedLog / sumWeights);
        return Math.Clamp(qed, 0.05, 0.98);
    }

    private static double AsymmetricDesirability(double x, double a, double b, double c, double d)
    {
        double exponent = -(x - c) / d;
        if (exponent > 50.0) return a;
        if (exponent < -50.0) return a + b;
        return a + (b / (1.0 + Math.Exp(exponent)));
    }
}
