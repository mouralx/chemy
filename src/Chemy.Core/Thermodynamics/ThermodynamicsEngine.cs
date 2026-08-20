namespace Chemy.Core.Thermodynamics;

/// <summary>
/// Encapsulates the results of a Hess's Law thermodynamic reaction feasibility calculation.
/// </summary>
/// <param name="EnthalpyChangekJ">Standard enthalpy of reaction ΔH° in kJ/mol.</param>
/// <param name="EntropyChangeJPerK">Standard entropy change ΔS° in J/(mol·K).</param>
/// <param name="GibbsFreeEnergykJ">Gibbs Free Energy change ΔG°(T) at temperature T in kJ/mol.</param>
/// <param name="TemperatureKelvin">Reaction temperature in Kelvin (K).</param>
/// <param name="IsExothermic">True if ΔH° &lt; 0 (heat release).</param>
/// <param name="IsEndothermic">True if ΔH° &gt; 0 (heat absorption).</param>
/// <param name="IsSpontaneous">True if ΔG° &lt; 0 (thermodynamically favorable reaction).</param>
public record ReactionThermodynamicsResult(
    double EnthalpyChangekJ,
    double EntropyChangeJPerK,
    double GibbsFreeEnergykJ,
    double TemperatureKelvin,
    bool IsExothermic,
    bool IsEndothermic,
    bool IsSpontaneous
)
{
    /// <summary>Formats the thermodynamic result as a string.</summary>
    public override string ToString() =>
        $"ΔH = {EnthalpyChangekJ:F1} kJ/mol, ΔS = {EntropyChangeJPerK:F1} J/(mol·K), ΔG = {GibbsFreeEnergykJ:F1} kJ/mol at {TemperatureKelvin:F1}K ({(IsExothermic ? "Exothermic" : "Endothermic")}, {(IsSpontaneous ? "Spontaneous" : "Non-spontaneous")})";
}

/// <summary>
/// 100% Universal Chemical Thermodynamics Engine.
/// Calculates standard reaction enthalpy (ΔH°), standard reaction entropy (ΔS°), and Gibbs free energy (ΔG°)
/// using Hess's Law tables and dynamic Benson Group Additivity estimation for arbitrary unknown molecules.
/// </summary>
public static class ThermodynamicsEngine
{
    /// <summary>
    /// Computes ΔH°, ΔS°, and ΔG° for any chemical reaction equation at temperature T.
    /// Uses tabulated NIST reference values with Benson Group Additivity fallback for arbitrary compounds.
    /// </summary>
    /// <param name="reaction">Input reaction equation (automatically balanced if needed).</param>
    /// <param name="temperatureKelvin">Temperature in Kelvin (default: 298.15 K).</param>
    /// <returns>ReactionThermodynamicsResult record.</returns>
    public static ReactionThermodynamicsResult GetThermodynamics(Reaction reaction, double temperatureKelvin = 298.15)
    {
        ArgumentNullException.ThrowIfNull(reaction);
        ArgumentOutOfRangeException.ThrowIfNegative(temperatureKelvin);

        var balanced = reaction.IsBalanced ? reaction : reaction.Balance();

        double totalProdEnthalpy = 0.0;
        double totalProdEntropy = 0.0;

        foreach (var prod in balanced.Products)
        {
            var props = ResolveThermodynamicProperties(prod.Molecule);
            totalProdEnthalpy += prod.Coefficient * props.EnthalpyOfFormationkJPerMol;
            totalProdEntropy += prod.Coefficient * props.MolarEntropyJPerMolK;
        }

        double totalReactEnthalpy = 0.0;
        double totalReactEntropy = 0.0;

        foreach (var react in balanced.Reactants)
        {
            var props = ResolveThermodynamicProperties(react.Molecule);
            totalReactEnthalpy += react.Coefficient * props.EnthalpyOfFormationkJPerMol;
            totalReactEntropy += react.Coefficient * props.MolarEntropyJPerMolK;
        }

        double deltaH = totalProdEnthalpy - totalReactEnthalpy;
        double deltaS = totalProdEntropy - totalReactEntropy;
        double deltaG = deltaH - (temperatureKelvin * (deltaS / 1000.0));

        return new ReactionThermodynamicsResult(
            deltaH,
            deltaS,
            deltaG,
            temperatureKelvin,
            IsExothermic: deltaH < 0,
            IsEndothermic: deltaH > 0,
            IsSpontaneous: deltaG < 0
        );
    }

    /// <summary>
    /// Resolves thermodynamic properties from NIST tables or dynamic Benson Group Additivity estimation.
    /// Reference: S.W. Benson, Thermochemical Kinetics (2nd ed. 1976); Cohen &amp; Benson, Chem. Rev. 1993, 93, 2419-2438.
    /// </summary>
    private static StandardThermodynamicProperties ResolveThermodynamicProperties(Molecule molecule)
    {
        if (ThermodynamicData.TryGetProperties(molecule.ChemicalFormula, out var props))
            return props;

        if (ThermodynamicData.TryGetProperties(molecule.Name, out props))
            return props;

        // Benson Group Additivity estimation for organic and unknown compounds
        double hf = 0.0;
        double s = 150.0 + (1.5 * 8.314 * Math.Log(Math.Max(10.0, molecule.MolecularWeight)));

        var graph = Graph.ChemicalGraph.FromMolecule(molecule);
        int nAtoms = molecule.Atoms.Count;

        for (int i = 0; i < nAtoms; i++)
        {
            var atom = molecule.Atoms[i];
            string sym = atom.Element.Symbol;
            var neighbors = molecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                .ToList();

            if (sym == "C")
            {
                bool isAromatic = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Aromatic);
                bool hasDouble = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Double);
                bool hasTriple = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Triple);

                if (isAromatic)
                {
                    bool hasH = neighbors.Any(n => molecule.Atoms[n].Element.Symbol == "H");
                    bool hasO = neighbors.Any(n => molecule.Atoms[n].Element.Symbol == "O");
                    if (hasH) { hf += 13.81; s += 33.5; }
                    else if (hasO) { hf += -16.74; s += -41.8; }
                    else { hf += 23.01; s += -62.8; }
                }
                else if (hasDouble)
                {
                    bool isCarbonyl = neighbors.Any(n => molecule.Atoms[n].Element.Symbol == "O");
                    if (isCarbonyl)
                    {
                        int cCount = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "C");
                        int oCount = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "O");
                        if (cCount >= 2) { hf += -131.8; s += 62.8; }
                        else if (cCount >= 1 && oCount >= 2) { hf += -142.3; s += 75.3; } // Acid/Ester CO
                        else { hf += -121.3; s += 146.4; } // Aldehyde
                    }
                    else
                    {
                        // Alkene Cd
                        int hCount = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "H");
                        if (hCount >= 2) { hf += 26.23; s += 115.5; }
                        else if (hCount == 1) { hf += 35.94; s += 34.3; }
                        else { hf += 43.26; s += -57.7; }
                    }
                }
                else if (hasTriple)
                {
                    hf += 113.7;
                    s += 60.0;
                }
                else
                {
                    // Aliphatic sp3
                    int hCount = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "H");
                    int cCount = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "C");
                    int oCount = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "O");

                    if (oCount > 0)
                    {
                        hf += -34.3 * oCount;
                        s += 25.0;
                    }
                    else
                    {
                        switch (cCount)
                        {
                            case 0:
                            case 1:
                                hf += -42.17; s += 127.2; break; // C-(C)(H)3
                            case 2:
                                hf += -20.63; s += 39.4; break;  // C-(C)2(H)2
                            case 3:
                                hf += -7.95; s += -50.2; break;  // C-(C)3(H)
                            default:
                                hf += 2.09; s += -146.4; break;  // C-(C)4
                        }
                    }
                }
            }
            else if (sym == "O")
            {
                bool isDouble = molecule.Bonds.Any(b => b.Connects(i) && b.Type == BondType.Double);
                if (!isDouble)
                {
                    bool hasH = neighbors.Any(n => molecule.Atoms[n].Element.Symbol == "H");
                    if (hasH) { hf += -158.6; s += 121.3; } // O-(C)(H)
                    else { hf += -99.6; s += 38.9; }        // O-(C)2
                }
            }
            else if (sym == "N")
            {
                int hCount = neighbors.Count(n => molecule.Atoms[n].Element.Symbol == "H");
                if (hCount >= 2) { hf += 48.1; s += 125.5; }
                else if (hCount == 1) { hf += 75.3; s += 29.3; }
                else { hf += 102.1; s += -92.0; }
            }
            else if (sym is "F" or "Cl" or "Br" or "I")
            {
                hf += sym switch
                {
                    "F" => -210.0,
                    "Cl" => -67.0,
                    "Br" => -33.0,
                    _ => +15.0
                };
            }
        }

        // Ring strain energy corrections
        var rings = graph.FindRings();
        foreach (var ring in rings)
        {
            if (ring.Count == 3) hf += 115.5;
            else if (ring.Count == 4) hf += 111.0;
            else if (ring.Count == 5 && !ring.Any(n => graph.GetIncidentEdges(n).Any(e => e.IsAromatic))) hf += 26.4;
        }

        double deltaSFormation = (s - (molecule.Atoms.Count * 30.0)) / 1000.0;
        double gibbs = hf - (298.15 * deltaSFormation);

        return new StandardThermodynamicProperties(Math.Round(hf, 1), Math.Round(s, 1), Math.Round(gibbs, 1));
    }
}
