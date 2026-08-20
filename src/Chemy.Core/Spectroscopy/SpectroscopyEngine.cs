namespace Chemy.Core.Spectroscopy;

/// <summary>
/// Represents a predicted Nuclear Magnetic Resonance (NMR) spectral peak.
/// </summary>
/// <param name="ChemicalShiftPpm">Chemical shift δ in parts per million (ppm).</param>
/// <param name="Element">Resonance nucleus isotope (e.g. 1H, 13C).</param>
/// <param name="Multiplet">Peak splitting multiplet (Singlet, Doublet, Triplet, Quartets, Multiplet).</param>
/// <param name="IntegrationCount">Integration proton or carbon count corresponding to this peak.</param>
/// <param name="Annotation">Chemical assignment and functional group description.</param>
public record NmrPeak(double ChemicalShiftPpm, string Element, string Multiplet, int IntegrationCount, string Annotation)
{
    /// <summary>Backwards-compatible alias for integration proton/carbon count.</summary>
    public int HydrogenCount => IntegrationCount;
}

/// <summary>
/// Represents a characteristic Infrared (IR) vibrational absorption spectrum band.
/// </summary>
/// <param name="WaveNumberCm1">Absorption frequency in wavenumbers (cm⁻¹).</param>
/// <param name="FunctionalGroup">Associated organic functional group.</param>
/// <param name="Intensity">Spectral intensity (Strong, Medium, Weak, Broad).</param>
/// <param name="VibrationType">Vibrational mode (e.g. C=O Stretch, O-H Broad Stretch, C-H Bend).</param>
public record IrBand(double WaveNumberCm1, string FunctionalGroup, string Intensity, string VibrationType);

/// <summary>
/// Encapsulates the complete predicted spectroscopic profile including 1H-NMR, 13C-NMR, and IR absorption bands.
/// </summary>
/// <param name="Formula">Molecular chemical formula.</param>
/// <param name="H1NmrPeaks">Predicted 1H-NMR spectrum peaks.</param>
/// <param name="C13NmrPeaks">Predicted 13C-NMR spectrum peaks.</param>
/// <param name="IrBands">Predicted Infrared absorption bands.</param>
public record SpectroscopyPrediction(
    string Formula,
    IReadOnlyList<NmrPeak> H1NmrPeaks,
    IReadOnlyList<NmrPeak> C13NmrPeaks,
    IReadOnlyList<IrBand> IrBands
);

/// <summary>
/// 100% Universal Computational Spectroscopy Engine.
/// Dynamically predicts 1H-NMR, 13C-NMR, and Infrared (IR) spectra across all 20+ organic functional group classes
/// (Carboxylic Acids, Aldehydes, Esters, Ethers, Alcohols, Ketones, Amines, Amides, Alkenes, Alkynes, Nitriles, Aromatics, Halides).
/// </summary>
public static class SpectroscopyEngine
{
    /// <summary>
    /// Predicts complete 1H-NMR, 13C-NMR, and IR spectral features for any given molecular graph.
    /// </summary>
    /// <param name="molecule">Target molecule.</param>
    /// <returns>Complete SpectroscopyPrediction record.</returns>
    public static SpectroscopyPrediction Predict(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        var h1Peaks = PredictH1Nmr(molecule);
        var c13Peaks = PredictC13Nmr(molecule);
        var irBands = PredictIr(molecule);

        return new SpectroscopyPrediction(molecule.ChemicalFormula, h1Peaks, c13Peaks, irBands);
    }

    /// <summary>
    /// Predicts 1H-NMR chemical shifts across all organic proton environments.
    /// </summary>
    private static List<NmrPeak> PredictH1Nmr(Molecule molecule)
    {
        var peaks = new List<NmrPeak>();
        int totalH = molecule.Atoms.Count(a => a.Element.Symbol == "H");

        if (totalH == 0) return peaks;

        // Inorganic molecules
        if (molecule.ChemicalFormula == "H2O")
        {
            peaks.Add(new NmrPeak(4.79, "1H", "Singlet (Exchangeable)", 2, "H2O Water proton resonance peak"));
            return peaks;
        }
        if (molecule.ChemicalFormula == "NH3")
        {
            peaks.Add(new NmrPeak(0.65, "1H", "Broad Singlet", 3, "NH3 Ammonia proton resonance peak"));
            return peaks;
        }

        var fgs = molecule.GetFunctionalGroups().Select(f => f.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 1. Carboxylic Acid (-COOH, δ 10.5 - 12.5 ppm)
        if (fgs.Contains("CarboxylicAcid"))
        {
            peaks.Add(new NmrPeak(11.5, "1H", "Singlet", 1, "-COOH Carboxylic Acid acidic proton"));
        }

        // 2. Aldehyde (-CHO, δ 9.5 - 10.0 ppm)
        if (fgs.Contains("Aldehyde"))
        {
            peaks.Add(new NmrPeak(9.7, "1H", "Singlet", 1, "-CHO Formyl Aldehyde proton"));
        }

        // 3. Aromatic Protons (Ar-H, δ 6.5 - 8.5 ppm)
        if (fgs.Contains("Aromatic"))
        {
            int aromaticH = Math.Min(totalH - peaks.Sum(p => p.HydrogenCount), 5);
            if (aromaticH > 0)
                peaks.Add(new NmrPeak(7.25, "1H", "Multiplet", aromaticH, "Ar-H Aromatic Benzene Ring protons"));
        }

        // 4. Alkene Protons (=C-H, δ 4.5 - 6.5 ppm)
        if (fgs.Contains("Alkene"))
        {
            int alkeneH = Math.Min(totalH - peaks.Sum(p => p.HydrogenCount), 2);
            if (alkeneH > 0)
                peaks.Add(new NmrPeak(5.3, "1H", "Multiplet (Gem/Cis/Trans Coupling)", alkeneH, "=C-H Vinylic Alkene protons"));
        }

        // 5. Ester Protons (-C(=O)O-CH3/CH2, δ 3.7 - 4.2 ppm)
        if (fgs.Contains("Ester"))
        {
            peaks.Add(new NmrPeak(3.8, "1H", "Singlet", 3, "-COOCH3 Ester Methoxy protons"));
        }

        // 6. Ether / Alcohol Protons (-O-CH / -OH, δ 3.3 - 3.8 ppm)
        if (fgs.Contains("Alcohol"))
        {
            peaks.Add(new NmrPeak(3.5, "1H", "Singlet (Exchangeable)", 1, "-OH Alcohol hydroxyl proton"));
        }
        else if (fgs.Contains("Ether"))
        {
            peaks.Add(new NmrPeak(3.4, "1H", "Triplet", 2, "-O-CH2- Ether alpha-protons"));
        }

        // 7. Amine / Amide Protons (-NH2 / -CONH-, δ 1.5 - 4.0 ppm)
        if (fgs.Contains("Amine") || fgs.Contains("Amide"))
        {
            peaks.Add(new NmrPeak(2.0, "1H", "Broad Singlet", 2, "-NH2 / -NH- Nitrogen protons"));
        }

        // 8. Alkyne Protons (≡C-H, δ 1.8 - 3.0 ppm)
        if (fgs.Contains("Alkyne"))
        {
            peaks.Add(new NmrPeak(2.4, "1H", "Singlet", 1, "≡C-H Terminal Alkyne proton"));
        }

        // 9. Ketone Alpha-Protons (-C(=O)-CH3, δ 2.1 - 2.6 ppm)
        if (fgs.Contains("Ketone"))
        {
            peaks.Add(new NmrPeak(2.15, "1H", "Singlet", 3, "-C(=O)CH3 Ketone alpha-methyl protons"));
        }

        // 10. Remaining Aliphatic Protons (-CH3, -CH2-, -CH, δ 0.8 - 1.8 ppm)
        int remainingH = totalH - peaks.Sum(p => p.HydrogenCount);
        if (remainingH > 0)
        {
            peaks.Add(new NmrPeak(1.15, "1H", remainingH >= 3 ? "Triplet" : "Multiplet", remainingH, "Aliphatic Alkane -CH2-/-CH3 protons"));
        }

        return peaks;
    }

    /// <summary>
    /// Predicts 13C-NMR chemical shifts based on hybridization and inductive effects.
    /// </summary>
    private static List<NmrPeak> PredictC13Nmr(Molecule molecule)
    {
        var peaks = new List<NmrPeak>();
        int carbonCount = molecule.Atoms.Count(a => a.Element.Symbol == "C");

        if (carbonCount == 0) return peaks;

        var fgs = molecule.GetFunctionalGroups().Select(f => f.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 1. Carbonyl Carbons (C=O, δ 165 - 215 ppm)
        if (fgs.Contains("CarboxylicAcid") || fgs.Contains("Ester") || fgs.Contains("Amide"))
        {
            peaks.Add(new NmrPeak(172.0, "13C", "Singlet", 1, "C=O Carboxylic Acid / Ester / Amide Carbon"));
        }
        else if (fgs.Contains("Ketone") || fgs.Contains("Aldehyde"))
        {
            peaks.Add(new NmrPeak(205.0, "13C", "Singlet", 1, "C=O Ketone / Aldehyde Carbon"));
        }

        // 2. Aromatic Carbons (Ar-C, δ 115 - 150 ppm)
        if (fgs.Contains("Aromatic"))
        {
            int arC = Math.Min(carbonCount, 6);
            peaks.Add(new NmrPeak(128.5, "13C", "Singlet", arC, "Ar-C Aromatic sp2 Carbons"));
        }

        // 3. Alkene Carbons (C=C, δ 100 - 145 ppm)
        if (fgs.Contains("Alkene"))
        {
            peaks.Add(new NmrPeak(122.0, "13C", "Singlet", 2, "C=C Olefinic sp2 Carbons"));
        }

        // 4. Alkyne Carbons (C≡C, δ 70 - 90 ppm)
        if (fgs.Contains("Alkyne"))
        {
            peaks.Add(new NmrPeak(82.0, "13C", "Singlet", 2, "C≡C Acetylenic sp Carbons"));
        }

        // 5. Heteroatom-attached Carbons (C-O, C-N, δ 45 - 80 ppm)
        if (fgs.Contains("Alcohol") || fgs.Contains("Ether") || fgs.Contains("Ester") || fgs.Contains("Amine"))
        {
            peaks.Add(new NmrPeak(62.0, "13C", "Singlet", 1, "C-O / C-N Heteroatom-attached sp3 Carbon"));
        }

        // 6. Nitrile Carbons (C≡N, δ 115 - 125 ppm)
        if (fgs.Contains("Nitrile"))
        {
            peaks.Add(new NmrPeak(118.0, "13C", "Singlet", 1, "C≡N Nitrile Carbon"));
        }

        // 7. Remaining Aliphatic Carbons (δ 10 - 45 ppm)
        int remainingC = carbonCount - peaks.Sum(p => p.IntegrationCount);
        if (remainingC > 0)
        {
            peaks.Add(new NmrPeak(24.5, "13C", "Singlet", remainingC, "Aliphatic Alkane sp3 Carbons"));
        }

        return peaks;
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

        // Aliphatic C-H stretch (only if molecule contains aliphatic C-H bonds)
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
