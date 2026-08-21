namespace Chemy.Core.Pharmacology;

using Chemy.Core.Graph;
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

    private static readonly AdsParams MwParams = new(2.817065973, 392.5754953, 290.7489764, 2.419764353, 49.22325677, 65.37051707, 104.9805561, 0.66);
    private static readonly AdsParams AlogpParams = new(3.172690585, 137.8624751, 2.534937431, 4.581497897, 0.822739154, 0.576295591, 131.3186604, 0.46);
    private static readonly AdsParams HbaParams = new(2.948620388, 160.4605972, 3.615294657, 4.435986202, 0.290141953, 1.300669958, 148.7763046, 0.05);
    private static readonly AdsParams HbdParams = new(1.618662227, 1010.051101, 0.985094388, 1e-09, 0.713820843, 0.920922555, 258.1632616, 0.61);
    private static readonly AdsParams TpsaParams = new(1.876861559, 125.2232657, 62.90773554, 87.83366614, 12.01999824, 28.51324732, 104.5686167, 0.06);
    private static readonly AdsParams RotbParams = new(0.01, 272.4121427, 2.55837997, 1.565547684, 1.271567166, 2.758063707, 105.4420403, 0.65);
    private static readonly AdsParams AromParams = new(3.21778897, 957.7374108, 2.274627939, 1e-09, 1.317690384, 0.375760881, 312.337261, 0.48);
    private static readonly AdsParams AlertsParams = new(0.01, 1199.094025, -0.09002883, 1e-09, 0.185904477, 0.875193782, 417.725314, 0.95);

    /// <summary>
    /// Computes the complete Bickerton QED score and descriptor desirabilities for a molecule.
    /// </summary>
    public static QedResult Calculate(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        if (!molecule.HasBondedTopology)
        {
            throw new InvalidOperationException(
                $"Molecule '{molecule.Name}' has no bonded topology. QED calculation requires a bonded molecular graph (e.g. from SMILES or Molfile/SDF), not an empirical formula without connectivity.");
        }

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
            ["HBA"] = CalculateAds(hba, HbaParams),
            ["HBD"] = CalculateAds(hbd, HbdParams),
            ["TPSA"] = CalculateAds(tpsa, TpsaParams),
            ["RotatableBonds"] = CalculateAds(rotb, RotbParams),
            ["AromaticRings"] = CalculateAds(arom, AromParams),
            ["StructuralAlerts"] = CalculateAds(alerts.Count, AlertsParams)
        };

        // Weighted geometric mean: QED = exp( sum(w_i * ln(d_i)) / sum(w_i) )
        double weightedLogSum = 
            MwParams.Weight * Math.Log(Math.Max(1e-4, dMap["MolecularWeight"])) +
            AlogpParams.Weight * Math.Log(Math.Max(1e-4, dMap["ALogP"])) +
            HbaParams.Weight * Math.Log(Math.Max(1e-4, dMap["HBA"])) +
            HbdParams.Weight * Math.Log(Math.Max(1e-4, dMap["HBD"])) +
            TpsaParams.Weight * Math.Log(Math.Max(1e-4, dMap["TPSA"])) +
            RotbParams.Weight * Math.Log(Math.Max(1e-4, dMap["RotatableBonds"])) +
            AromParams.Weight * Math.Log(Math.Max(1e-4, dMap["AromaticRings"])) +
            AlertsParams.Weight * Math.Log(Math.Max(1e-4, dMap["StructuralAlerts"]));

        double totalWeight = MwParams.Weight + AlogpParams.Weight + HbaParams.Weight + 
                             HbdParams.Weight + TpsaParams.Weight + RotbParams.Weight + 
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
        double exp1 = 1.0 + Math.Exp(-1.0 * (x - p.C + p.D / 2.0) / p.E);
        double exp2 = 1.0 + Math.Exp(-1.0 * (x - p.C - p.D / 2.0) / p.F);
        double dx = p.A + (p.B / exp1) * (1.0 - 1.0 / exp2);
        return Math.Clamp(dx / p.Dmax, 0.0001, 1.0);
    }

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
                if (h > 0) count++;
            }
        }
        return count;
    }

    private static int CountHba(Molecule molecule)
    {
        int count = 0;
        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var a = molecule.Atoms[i];
            if (a.NetCharge > 0) continue;

            if (a.Element.Symbol == "O")
            {
                bool hasHydrogen = molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H");
                bool isCarboxylicHydroxyl = hasHydrogen && molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b2 => b2.Connects(b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) && b2.Type == BondType.Double &&
                        molecule.Atoms[b2.Atom1Index == (b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) ? b2.Atom2Index : b2.Atom1Index].Element.Symbol == "O"));

                if (!isCarboxylicHydroxyl) count++;
            }
            else if (a.Element.Symbol == "N")
            {
                bool isAmide = molecule.Bonds.Any(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b2 => b2.Connects(b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) && b2.Type == BondType.Double &&
                        molecule.Atoms[b2.Atom1Index == (b.Atom1Index == i ? b.Atom2Index : b.Atom1Index) ? b2.Atom2Index : b2.Atom1Index].Element.Symbol == "O"));
                int oxygenNeighbors = molecule.Bonds.Count(b => b.Connects(i) && molecule.Atoms[b.Atom1Index == i ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O");
                bool isNitro = oxygenNeighbors >= 2;

                if (!isAmide && !isNitro) count++;
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

            var sssr = CycleBasis.ComputeSssr(molecule);
            if (sssr.Rings.Any(r => r.Contains(bond.Atom1Index) && r.Contains(bond.Atom2Index))) continue;

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

    private static int CountAromaticRings(Molecule molecule)
    {
        var sssr = CycleBasis.ComputeSssr(molecule);
        int arom = sssr.Rings.Count(r => r.All(atomIdx => molecule.Bonds.Any(b => b.Connects(atomIdx) && b.Type == BondType.Aromatic)));
        if (arom == 0 && molecule.Bonds.Any(b => b.Type == BondType.Aromatic)) arom = 1;
        return arom;
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
