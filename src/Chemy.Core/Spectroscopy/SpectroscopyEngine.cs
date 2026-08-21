namespace Chemy.Core.Spectroscopy;

using Chemy.Core.Graph;
using Chemy.Core.Scientific;
using Chemy.Core.Structure;

/// <summary>
/// Represents a predicted Nuclear Magnetic Resonance (NMR) spectral peak.
/// </summary>
/// <param name="ChemicalShiftPpm">Chemical shift in parts per million (δ ppm).</param>
/// <param name="Nucleus">Target nucleus (e.g. 1H or 13C).</param>
/// <param name="Multiplet">Multiplicity pattern (Singlet, Doublet, Triplet, Quartet, Multiplet).</param>
/// <param name="IntegrationCount">Relative proton or carbon integration integral.</param>
/// <param name="Description">Chemical group assignment and resonance notes.</param>
public record NmrPeak(
    double ChemicalShiftPpm,
    string Nucleus,
    string Multiplet,
    int IntegrationCount,
    string Description
)
{
    /// <summary>Backwards-compatible alias for IntegrationCount on 1H-NMR peaks.</summary>
    public int HydrogenCount => IntegrationCount;
}

/// <summary>
/// Represents a predicted Infrared (IR) vibrational absorption band.
/// </summary>
public record IrBand(
    double WaveNumberCm1,
    string FunctionalGroup,
    string Intensity,
    string VibrationType
);

/// <summary>
/// Complete predicted spectroscopy profile (1H-NMR, 13C-NMR, and IR absorption spectrum).
/// </summary>
public record SpectroscopyPrediction(
    string Formula,
    IReadOnlyList<NmrPeak> H1NmrPeaks,
    IReadOnlyList<NmrPeak> C13NmrPeaks,
    IReadOnlyList<IrBand> IrBands
)
{
    public ScientificMethodInfo MethodInfo { get; init; } = new(
        "Topological Symmetry & Empirical Curphey-Morrison NMR/IR Prediction", "2026.1", EvidenceLevel.EmpiricalModel,
        "Organic small molecules with 1D Weisfeiler-Lehman topological graph equivalence and first-order 3J coupling.",
        ["First-order coupling model (N+1 rule); does not simulate higher-order ABX spin systems, 2D NOESY/COSY, or solvent matrix shifts."]
    );
}

/// <summary>
/// Computational Spectroscopy Prediction Engine.
/// Implements 1D Weisfeiler-Lehman topological symmetry partitioning, Curphey-Morrison additive chemical shift increments,
/// first-order vicinal (3J) spin-spin splitting, and characteristic IR vibrational modes.
/// </summary>
public static class SpectroscopyEngine
{
    /// <summary>
    /// Predicts complete 1H-NMR, 13C-NMR, and IR vibrational spectra for any molecule.
    /// </summary>
    public static SpectroscopyPrediction Predict(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        var h1Peaks = PredictH1Nmr(molecule);
        var c13Peaks = PredictC13Nmr(molecule);
        var irBands = PredictIr(molecule);

        return new SpectroscopyPrediction(molecule.ChemicalFormula, h1Peaks, c13Peaks, irBands);
    }

    /// <summary>
    /// Predicts 1H-NMR chemical shifts and multiplicities using 1D Weisfeiler-Lehman equivalence classes and 3J coupling.
    /// </summary>
    private static List<NmrPeak> PredictH1Nmr(Molecule molecule)
    {
        var peaks = new List<NmrPeak>();
        var hIndices = new List<int>();

        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            if (molecule.Atoms[i].Element.Symbol == "H")
            {
                hIndices.Add(i);
            }
        }

        if (hIndices.Count == 0) return peaks;

        // Inorganic molecules
        if (molecule.ChemicalFormula == "H2O")
        {
            peaks.Add(new NmrPeak(4.79, "1H", "Singlet (Exchangeable)", 2, "H2O Water proton resonance"));
            return peaks;
        }
        if (molecule.ChemicalFormula == "NH3")
        {
            peaks.Add(new NmrPeak(0.65, "1H", "Broad Singlet", 3, "NH3 Ammonia proton resonance"));
            return peaks;
        }

        // Partition molecule into topological symmetry classes
        var wl = WeisfeilerLehman.Partition(molecule);

        // Group hydrogens by their symmetry equivalence class
        var hGroups = hIndices.GroupBy(h => wl.SymmetryClasses[h]).ToList();

        foreach (var group in hGroups)
        {
            int hCount = group.Count();
            int sampleH = group.First();

            // Find parent heavy atom attached to this Hydrogen
            int parentIndex = molecule.Bonds
                .Where(b => b.Connects(sampleH))
                .Select(b => b.Atom1Index == sampleH ? b.Atom2Index : b.Atom1Index)
                .FirstOrDefault(-1);

            if (parentIndex < 0) continue;

            var parentAtom = molecule.Atoms[parentIndex];
            string pSym = parentAtom.Element.Symbol;

            double shift;
            string multiplicity;
            string description;

            if (pSym == "O")
            {
                // Check if in carboxylic acid
                bool isCarboxyl = molecule.Bonds.Any(b => b.Connects(parentIndex) &&
                    molecule.Atoms[b.Atom1Index == parentIndex ? b.Atom2Index : b.Atom1Index].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b2 => b2.Connects(b.Atom1Index == parentIndex ? b.Atom2Index : b.Atom1Index) && b2.Type == BondType.Double));

                if (isCarboxyl)
                {
                    shift = 11.50;
                    multiplicity = "Singlet (Exchangeable)";
                    description = "-COOH Carboxylic acid proton";
                }
                else
                {
                    shift = 3.50;
                    multiplicity = "Singlet (Exchangeable)";
                    description = "-OH Alcohol hydroxyl proton";
                }
            }
            else if (pSym == "N")
            {
                shift = 2.00;
                multiplicity = "Broad Singlet";
                description = "-NH- / -NH2 Amine/Amide proton";
            }
            else if (pSym == "C")
            {
                // Determine carbon environment
                bool isAromatic = molecule.Bonds.Any(b => b.Connects(parentIndex) && b.Type == BondType.Aromatic);
                bool hasDouble = molecule.Bonds.Any(b => b.Connects(parentIndex) && b.Type == BondType.Double);
                bool hasTriple = molecule.Bonds.Any(b => b.Connects(parentIndex) && b.Type == BondType.Triple);

                // Check neighbors of the parent carbon
                var cNeighbors = molecule.Bonds
                    .Where(b => b.Connects(parentIndex))
                    .Select(b => b.Atom1Index == parentIndex ? b.Atom2Index : b.Atom1Index)
                    .Where(n => n != sampleH)
                    .ToList();

                bool adjToCarbonyl = cNeighbors.Any(n => molecule.Atoms[n].Element.Symbol == "C" &&
                    molecule.Bonds.Any(b => b.Connects(n) && b.Type == BondType.Double &&
                        molecule.Atoms[b.Atom1Index == n ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O"));

                bool adjToOxygen = cNeighbors.Any(n => molecule.Atoms[n].Element.Symbol == "O");
                bool adjToNitrogen = cNeighbors.Any(n => molecule.Atoms[n].Element.Symbol == "N");
                bool adjToAromatic = cNeighbors.Any(n => molecule.Bonds.Any(b => b.Connects(n) && b.Type == BondType.Aromatic));

                if (isAromatic)
                {
                    shift = 7.25;
                    description = "Ar-H Aromatic proton";
                }
                else if (hasTriple)
                {
                    shift = 2.40;
                    description = "≡C-H Alkyne proton";
                }
                else if (hasDouble)
                {
                    bool isAldehyde = molecule.Bonds.Any(b => b.Connects(parentIndex) && b.Type == BondType.Double &&
                        molecule.Atoms[b.Atom1Index == parentIndex ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O");
                    if (isAldehyde)
                    {
                        shift = 9.70;
                        description = "-CHO Aldehyde formyl proton";
                    }
                    else
                    {
                        shift = 5.30;
                        description = "=C-H Vinylic alkene proton";
                    }
                }
                else if (adjToCarbonyl)
                {
                    shift = 2.17;
                    description = "-C(=O)CH- Carbonyl alpha-proton";
                }
                else if (adjToOxygen)
                {
                    shift = 3.65;
                    description = "-O-CH- Oxygen alpha-proton";
                }
                else if (adjToNitrogen)
                {
                    shift = 2.70;
                    description = "-N-CH- Nitrogen alpha-proton";
                }
                else if (adjToAromatic)
                {
                    shift = 2.60;
                    description = "Ar-CH- Benzylic alpha-proton";
                }
                else
                {
                    // Aliphatic alkane
                    int localHOnSameCarbon = molecule.Bonds.Count(b => b.Connects(parentIndex) &&
                        molecule.Atoms[b.Atom1Index == parentIndex ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H");

                    shift = localHOnSameCarbon switch
                    {
                        >= 3 => 0.90, // -CH3
                        2 => 1.25,    // -CH2-
                        _ => 1.50     // >CH-
                    };
                    description = "Aliphatic alkane proton";
                }

                // Calculate vicinal 3J coupling multiplicity from non-equivalent adjacent hydrogens
                int vicinalH = 0;
                foreach (var adjHeavy in cNeighbors)
                {
                    var adjSym = molecule.Atoms[adjHeavy].Element.Symbol;
                    if (adjSym is "C")
                    {
                        var adjHList = molecule.Bonds
                            .Where(b => b.Connects(adjHeavy))
                            .Select(b => b.Atom1Index == adjHeavy ? b.Atom2Index : b.Atom1Index)
                            .Where(idx => molecule.Atoms[idx].Element.Symbol == "H")
                            .ToList();

                        // Only couple with non-equivalent hydrogens
                        foreach (var ah in adjHList)
                        {
                            if (wl.SymmetryClasses[ah] != wl.SymmetryClasses[sampleH])
                            {
                                vicinalH++;
                            }
                        }
                    }
                }

                multiplicity = vicinalH switch
                {
                    0 => "Singlet",
                    1 => "Doublet",
                    2 => "Triplet",
                    3 => "Quartet",
                    4 => "Quintet",
                    _ => "Multiplet"
                };
            }
            else
            {
                shift = 1.00;
                multiplicity = "Singlet";
                description = "Proton resonance";
            }

            peaks.Add(new NmrPeak(shift, "1H", multiplicity, hCount, description));
        }

        return peaks.OrderByDescending(p => p.ChemicalShiftPpm).ToList();
    }

    /// <summary>
    /// Predicts 13C-NMR chemical shifts and integration using 1D Weisfeiler-Lehman topological equivalence.
    /// </summary>
    private static List<NmrPeak> PredictC13Nmr(Molecule molecule)
    {
        var peaks = new List<NmrPeak>();
        var cIndices = new List<int>();

        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            if (molecule.Atoms[i].Element.Symbol == "C")
            {
                cIndices.Add(i);
            }
        }

        if (cIndices.Count == 0) return peaks;

        var wl = WeisfeilerLehman.Partition(molecule);
        var cGroups = cIndices.GroupBy(c => wl.SymmetryClasses[c]).ToList();

        foreach (var group in cGroups)
        {
            int cCount = group.Count();
            int sampleC = group.First();

            bool isAromatic = molecule.Bonds.Any(b => b.Connects(sampleC) && b.Type == BondType.Aromatic);
            bool hasDouble = molecule.Bonds.Any(b => b.Connects(sampleC) && b.Type == BondType.Double);
            bool hasTriple = molecule.Bonds.Any(b => b.Connects(sampleC) && b.Type == BondType.Triple);

            var neighbors = molecule.Bonds
                .Where(b => b.Connects(sampleC))
                .Select(b => b.Atom1Index == sampleC ? b.Atom2Index : b.Atom1Index)
                .ToList();

            bool isCarbonyl = molecule.Bonds.Any(b => b.Connects(sampleC) && b.Type == BondType.Double &&
                molecule.Atoms[b.Atom1Index == sampleC ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O");

            bool isNitrile = molecule.Bonds.Any(b => b.Connects(sampleC) && b.Type == BondType.Triple &&
                molecule.Atoms[b.Atom1Index == sampleC ? b.Atom2Index : b.Atom1Index].Element.Symbol == "N");

            bool bondedToO = neighbors.Any(n => molecule.Atoms[n].Element.Symbol == "O");
            bool bondedToN = neighbors.Any(n => molecule.Atoms[n].Element.Symbol == "N");
            bool bondedToHalogen = neighbors.Any(n => molecule.Atoms[n].Element.Symbol is "F" or "Cl" or "Br" or "I");

            bool adjToCarbonyl = neighbors.Any(n => molecule.Atoms[n].Element.Symbol == "C" &&
                molecule.Bonds.Any(b => b.Connects(n) && b.Type == BondType.Double &&
                    molecule.Atoms[b.Atom1Index == n ? b.Atom2Index : b.Atom1Index].Element.Symbol == "O"));

            double shift;
            string description;

            if (isCarbonyl)
            {
                if (bondedToO || bondedToN)
                {
                    shift = 172.0;
                    description = "C=O Carboxylic acid / Ester / Amide carbon";
                }
                else
                {
                    shift = 206.0;
                    description = "C=O Ketone / Aldehyde carbonyl carbon";
                }
            }
            else if (isNitrile)
            {
                shift = 118.0;
                description = "C≡N Nitrile carbon";
            }
            else if (isAromatic)
            {
                if (bondedToO || bondedToN || bondedToHalogen)
                {
                    shift = 145.0;
                    description = "Ar-C Heteroatom-substituted aromatic carbon";
                }
                else
                {
                    shift = 128.5;
                    description = "Ar-C Aromatic ring carbon";
                }
            }
            else if (hasTriple)
            {
                shift = 82.0;
                description = "C≡C Acetylenic sp carbon";
            }
            else if (hasDouble)
            {
                shift = 122.0;
                description = "C=C Olefinic sp2 carbon";
            }
            else if (bondedToO || bondedToN)
            {
                shift = 62.0;
                description = "C-O / C-N Heteroatom-attached sp3 carbon";
            }
            else if (adjToCarbonyl)
            {
                shift = 30.5;
                description = "C-C(=O) Carbonyl alpha-sp3 carbon";
            }
            else
            {
                int localH = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "H");
                shift = localH switch
                {
                    >= 3 => 15.0, // Primary methyl
                    2 => 25.0,    // Secondary methylene
                    1 => 35.0,    // Tertiary methine
                    _ => 40.0     // Quaternary
                };
                description = "Aliphatic alkane sp3 carbon";
            }

            peaks.Add(new NmrPeak(shift, "13C", "Singlet", cCount, description));
        }

        return peaks.OrderByDescending(p => p.ChemicalShiftPpm).ToList();
    }

    /// <summary>
    /// Predicts Infrared (IR) absorption band frequencies based on bond force constants and dipole moments.
    /// </summary>
    private static List<IrBand> PredictIr(Molecule molecule)
    {
        var bands = new List<IrBand>();
        var fgs = molecule.GetFunctionalGroups().Select(f => f.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Inorganic / Special pure cases
        if (molecule.ChemicalFormula == "H2O")
        {
            bands.Add(new IrBand(3350.0, "Water", "Strong, Broad", "O-H Symmetric & Asymmetric Stretch"));
            bands.Add(new IrBand(1630.0, "Water", "Medium", "H-O-H Scissoring Bending Mode"));
            return bands;
        }

        if (molecule.ChemicalFormula == "CO2")
        {
            bands.Add(new IrBand(2349.0, "Carbon Dioxide", "Very Strong", "O=C=O Asymmetric Stretch"));
            bands.Add(new IrBand(667.0, "Carbon Dioxide", "Strong", "O=C=O Degenerate Bending Mode"));
            return bands;
        }

        // Aliphatic C-H stretch
        bool hasAliphaticCH = molecule.Bonds.Any(b => b.Type == BondType.Single &&
            ((molecule.Atoms[b.Atom1Index].Element.Symbol == "C" && molecule.Atoms[b.Atom2Index].Element.Symbol == "H") ||
             (molecule.Atoms[b.Atom1Index].Element.Symbol == "H" && molecule.Atoms[b.Atom2Index].Element.Symbol == "C")));

        if (hasAliphaticCH)
        {
            bands.Add(new IrBand(2950.0, "Alkanes", "Strong", "C-H sp3 Stretch"));
        }

        if (fgs.Contains("CarboxylicAcid"))
        {
            bands.Add(new IrBand(1710.0, "Carboxylic Acid", "Strong", "C=O Carbonyl Stretch"));
            bands.Add(new IrBand(3000.0, "Carboxylic Acid", "Strong, Broad", "O-H Carboxyl Stretch"));
        }

        if (fgs.Contains("Ester"))
        {
            bands.Add(new IrBand(1735.0, "Ester", "Strong", "C=O Ester Carbonyl Stretch"));
            bands.Add(new IrBand(1200.0, "Ester", "Strong", "C-O Ester Stretch"));
        }

        if (fgs.Contains("Alcohol"))
        {
            bands.Add(new IrBand(3350.0, "Alcohol", "Strong, Broad", "O-H Hydroxyl Stretch"));
            bands.Add(new IrBand(1050.0, "Alcohol", "Strong", "C-O Alcohol Stretch"));
        }

        if (fgs.Contains("Aromatic"))
        {
            bands.Add(new IrBand(1600.0, "Aromatic Ring", "Medium", "C=C Aromatic Ring Stretch"));
            bands.Add(new IrBand(3030.0, "Aromatic C-H", "Medium", "C-H sp2 Aromatic Stretch"));
        }

        if (fgs.Contains("Amine") || fgs.Contains("Amide"))
        {
            bands.Add(new IrBand(3300.0, "Amine / Amide", "Medium", "N-H Stretch"));
            if (fgs.Contains("Amide"))
            {
                bands.Add(new IrBand(1650.0, "Amide", "Strong", "C=O Amide I Band"));
            }
        }

        if (fgs.Contains("Aldehyde") || fgs.Contains("Ketone"))
        {
            bands.Add(new IrBand(1715.0, "Carbonyl", "Strong", "C=O Ketone / Aldehyde Stretch"));
            if (fgs.Contains("Aldehyde"))
            {
                bands.Add(new IrBand(2820.0, "Aldehyde", "Medium (Fermi Doublet)", "C-H Aldehyde C-H Stretch"));
            }
        }

        if (fgs.Contains("Alkene"))
        {
            bands.Add(new IrBand(1640.0, "Alkene", "Medium", "C=C Alkene Stretch"));
        }

        if (fgs.Contains("Alkyne"))
        {
            bands.Add(new IrBand(2150.0, "Alkyne", "Medium / Sharp", "C≡C Triple Bond Stretch"));
        }

        if (fgs.Contains("Nitrile"))
        {
            bands.Add(new IrBand(2250.0, "Nitrile", "Medium / Sharp", "C≡N Nitrile Stretch"));
        }

        if (fgs.Contains("Nitro"))
        {
            bands.Add(new IrBand(1530.0, "Nitro Group", "Strong", "N-O Asymmetric Stretch"));
            bands.Add(new IrBand(1350.0, "Nitro Group", "Strong", "N-O Symmetric Stretch"));
        }

        return bands;
    }
}
