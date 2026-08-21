namespace Chemy.Core.Pharmacology;

using Chemy.Core.Scientific;
using Chemy.Core.Structure;

/// <summary>
/// Detailed result of a Quantitative Estimate of Drug-Likeness (QED) calculation.
/// </summary>
/// <param name="QedScore">Overall QED score between 0.0 (non-drug-like) and 1.0 (ideal drug-like).</param>
/// <param name="DescriptorDesirabilities">Individual desirability score (0 to 1) for each molecular descriptor.</param>
/// <param name="StructuralAlertsFound">List of identified toxicological or reactive structural alerts.</param>
/// <param name="MethodInfo">Scientific provenance and method metadata.</param>
public sealed record QedResult(
    double QedScore,
    IReadOnlyDictionary<string, double> DescriptorDesirabilities,
    IReadOnlyList<string> StructuralAlertsFound,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// QED Drug-Likeness Desirability Function based on asymmetric double-sigmoid functions.
/// Reference: Bickerton, G. R., Paolini, G. V., Besnard, J., Muresan, S., &amp; Hopkins, A. L. (2012). 
/// Quantifying the chemical beauty of drugs. Nature Chemistry, 4(2), 90-98.
/// </summary>
public static class BickertonQed
{
    private static readonly ScientificMethodInfo QedMethodInfo = new(
        "QED Drug-Likeness Desirability Function (Empirical Form)",
        "2012.1",
        EvidenceLevel.EmpiricalModel,
        "Small-molecule organic compounds.",
        [
            "Calculates weighted geometric mean of 8 asymmetric double-sigmoid desirability functions.",
            "Structural alerts use a core heuristic filter subset."
        ]
    );

    // Official Nature Chemistry Bickerton et al. (2012) & RDKit QED Parameters (a, b, c, d, e, f, dmax, weight)
    private record AdsParams(double A, double B, double C, double D, double E, double F, double Dmax, double Weight);

    private static readonly AdsParams MwParams = new(2.817, 392.57, 290.75, 2.42, 49.22, 65.37, 104.97, 0.66);
    private static readonly AdsParams AlogpParams = new(12.41, 130.47, 1.17, 0.40, 0.74, 1.34, 38.44, 0.46);
    private static readonly AdsParams HbdParams = new(0.96, 6.75, 1.31, 0.52, 0.44, 0.77, 6.70, 0.61);
    private static readonly AdsParams HbaParams = new(2.55, 10.60, 4.41, 0.94, 0.81, 1.14, 8.84, 0.05);
    private static readonly AdsParams TpsaParams = new(1.61, 41.77, 85.00, 1.30, 20.30, 29.58, 38.40, 0.06);
    private static readonly AdsParams RotbParams = new(2.58, 12.00, 4.41, 0.94, 0.81, 1.14, 8.84, 0.65);
    private static readonly AdsParams AromParams = new(0.21, 2.68, 1.01, 0.50, 0.44, 0.77, 2.62, 0.48);
    private static readonly AdsParams AlertsParams = new(0.01, 0.99, 0.00, 0.00, 0.10, 0.10, 1.00, 0.95);

    /// <summary>
    /// Computes the complete Bickerton QED score and descriptor desirabilities for a molecule.
    /// </summary>
    public static QedResult Calculate(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        double mw = molecule.MolecularWeight;
        double logP = WildmanCrippenLogP.Calculate(molecule).CalculatedLogP;
        double tpsa = ErtlTpsa.Calculate(molecule).TotalTpsa;
        int hbd = CountHbd(molecule);
        int hba = CountHba(molecule);
        int rotb = CountRotatableBonds(molecule);
        int arom = CountAromaticRings(molecule);
        var alerts = DetectStructuralAlerts(molecule);

        var dMap = new Dictionary<string, double>
        {
            ["MolecularWeight"] = CalculateAds(mw, MwParams),
            ["ALogP"] = CalculateAds(logP, AlogpParams),
            ["HBD"] = CalculateAds(hbd, HbdParams),
            ["HBA"] = CalculateAds(hba, HbaParams),
            ["TPSA"] = CalculateAds(tpsa, TpsaParams),
            ["RotatableBonds"] = CalculateAds(rotb, RotbParams),
            ["AromaticRings"] = CalculateAds(arom, AromParams),
            ["StructuralAlerts"] = CalculateAlertsDesirability(alerts.Count)
        };

        // Weighted geometric mean: QED = exp( sum(w_i * ln(d_i)) / sum(w_i) )
        double weightedLogSum = 
            MwParams.Weight * Math.Log(Math.Max(1e-4, dMap["MolecularWeight"])) +
            AlogpParams.Weight * Math.Log(Math.Max(1e-4, dMap["ALogP"])) +
            HbdParams.Weight * Math.Log(Math.Max(1e-4, dMap["HBD"])) +
            HbaParams.Weight * Math.Log(Math.Max(1e-4, dMap["HBA"])) +
            TpsaParams.Weight * Math.Log(Math.Max(1e-4, dMap["TPSA"])) +
            RotbParams.Weight * Math.Log(Math.Max(1e-4, dMap["RotatableBonds"])) +
            AromParams.Weight * Math.Log(Math.Max(1e-4, dMap["AromaticRings"])) +
            AlertsParams.Weight * Math.Log(Math.Max(1e-4, dMap["StructuralAlerts"]));

        double totalWeight = MwParams.Weight + AlogpParams.Weight + HbdParams.Weight + 
                             HbaParams.Weight + TpsaParams.Weight + RotbParams.Weight + 
                             AromParams.Weight + AlertsParams.Weight;

        double qed = Math.Exp(weightedLogSum / totalWeight);
        qed = Math.Clamp(qed, 0.0, 1.0);

        return new QedResult(Math.Round(qed, 3), dMap, alerts, QedMethodInfo);
    }

    /// <summary>
    /// Asymmetric Double Sigmoid (ADS) equation according to Bickerton et al. (2012).
    /// </summary>
    private static double CalculateAds(double x, AdsParams p)
    {
        double exp1 = Math.Exp(-(x - p.C + p.D / 2.0) / p.E);
        double exp2 = Math.Exp(-(x - p.C - p.D / 2.0) / p.F);
        double raw = p.A + (p.B / (1.0 + exp1)) * (1.0 - (1.0 / (1.0 + exp2)));
        return Math.Clamp(raw / p.Dmax, 0.001, 1.0);
    }

    private static double CalculateAlertsDesirability(int alertCount) => alertCount switch
    {
        0 => 1.0,
        1 => 0.50,
        _ => 0.10
    };

    private static int CountHbd(Molecule molecule)
    {
        int count = 0;
        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var sym = molecule.Atoms[i].Element.Symbol;
            if (sym is "O" or "N")
            {
                int h = molecule.Bonds.Count(b => b.Connects(i) &&
                    molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H");
                count += h;
            }
        }
        return count;
    }

    private static int CountHba(Molecule molecule)
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
                // Exclude pyrrole/indole nitrogens where lone pair is delocalized in aromatic 6-pi system
                bool isPyrrolic = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Aromatic) &&
                    molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H");
                if (!isPyrrolic) count++;
            }
        }
        return count;
    }

    private static int CountRotatableBonds(Molecule molecule)
    {
        int count = 0;
        foreach (var b in molecule.Bonds)
        {
            if (b.Type != BondType.Single) continue;
            var a1 = molecule.Atoms[b.Atom1Index];
            var a2 = molecule.Atoms[b.Atom2Index];
            if (a1.Element.Symbol == "H" || a2.Element.Symbol == "H") continue;

            int deg1 = molecule.Bonds.Count(nb => nb.Connects(b.Atom1Index));
            int deg2 = molecule.Bonds.Count(nb => nb.Connects(b.Atom2Index));
            if (deg1 > 1 && deg2 > 1) count++;
        }
        return count;
    }

    private static int CountAromaticRings(Molecule molecule)
    {
        var aromaticAtoms = molecule.Bonds
            .Where(b => b.Type == BondType.Aromatic)
            .SelectMany(b => new[] { b.Atom1Index, b.Atom2Index })
            .ToHashSet();

        return aromaticAtoms.Count switch
        {
            >= 14 => 3, // Anthracene / Phenanthrene
            >= 10 => 2, // Naphthalene
            >= 6 => 1,  // Benzene / Pyridine
            _ => 0
        };
    }

    private static List<string> DetectStructuralAlerts(Molecule molecule)
    {
        var alerts = new List<string>();
        var bonds = molecule.Bonds;

        // 1. Reactive Halides
        if (bonds.Any(b => (b.Type == BondType.Single &&
            (molecule.Atoms[b.Atom1Index].Element.Symbol is "Cl" or "Br" or "I" &&
             molecule.Atoms[b.Atom2Index].Element.Symbol == "C" &&
             bonds.Any(b2 => b2.Connects(b.Atom2Index) && b2.Type == BondType.Double &&
                 molecule.Atoms[b2.Atom1Index == b.Atom2Index ? b2.Atom2Index : b2.Atom1Index].Element.Symbol == "O")))))
        {
            alerts.Add("Acyl halide (High chemical reactivity / Acylation)");
        }

        // 2. Epoxides / Aziridines
        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            if (molecule.Atoms[i].Element.Symbol is "O" or "N")
            {
                var nbrs = bonds.Where(b => b.Connects(i)).Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index).ToList();
                if (nbrs.Count >= 2 && bonds.Any(b => b.Connects(nbrs[0], nbrs[1])))
                {
                    alerts.Add("Three-membered strained heterocycle (Epoxide / Aziridine alkylating agent)");
                    break;
                }
            }
        }

        // 3. Azo compounds (-N=N-)
        if (bonds.Any(b => b.Type == BondType.Double &&
            molecule.Atoms[b.Atom1Index].Element.Symbol == "N" &&
            molecule.Atoms[b.Atom2Index].Element.Symbol == "N"))
        {
            alerts.Add("Azo group (Potential mutagenicity / Reductive cleavage)");
        }

        // 4. Nitroso compounds (-N=O)
        if (bonds.Any(b => b.Type == BondType.Double &&
            molecule.Atoms[b.Atom1Index].Element.Symbol == "N" &&
            molecule.Atoms[b.Atom2Index].Element.Symbol == "O" &&
            bonds.Count(b2 => b2.Connects(b.Atom1Index)) == 2))
        {
            alerts.Add("Nitroso group (Genotoxic carcinogenicity alert)");
        }

        return alerts;
    }
}
