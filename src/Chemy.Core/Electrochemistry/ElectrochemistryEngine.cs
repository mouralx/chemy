namespace Chemy.Core.Electrochemistry;

/// <summary>
/// Encapsulates the results of non-standard electrochemical cell potential calculations.
/// </summary>
/// <param name="CellPotentialVolts">Non-standard cell potential E_cell in Volts (V).</param>
/// <param name="StandardCellPotentialVolts">Standard reduction cell potential E°_cell in Volts (V).</param>
/// <param name="ElectronsTransferred">Number of moles of electrons transferred in balanced redox reaction (n).</param>
/// <param name="ReactionQuotientQ">Reaction quotient Q.</param>
/// <param name="TemperatureKelvin">Temperature in Kelvin (K).</param>
/// <param name="IsSpontaneousGalvanic">True if E_cell &gt; 0 (spontaneous galvanic discharge).</param>
public record NernstResult(
    double CellPotentialVolts,
    double StandardCellPotentialVolts,
    int ElectronsTransferred,
    double ReactionQuotientQ,
    double TemperatureKelvin,
    bool IsSpontaneousGalvanic
)
{
    /// <summary>Formats the Nernst calculation result as a string.</summary>
    public override string ToString() => $"E_cell = {CellPotentialVolts:F3} V (E° = {StandardCellPotentialVolts:F3} V, n = {ElectronsTransferred})";
}

/// <summary>
/// Textbook Electrochemistry &amp; Nernst Cell Potential Engine.
/// Calculates non-standard cell potentials ($E_{\text{cell}}$), Faraday redox transfer, and galvanic spontaneity.
/// </summary>
public static class ElectrochemistryEngine
{
    /// <summary>Ideal gas constant R in J/(mol·K).</summary>
    private const double GasConstantR = 8.314462618;

    /// <summary>Faraday constant F in Coulombs per mole of electrons (C/mol).</summary>
    private const double FaradayConstantF = 96485.33212;

    /// <summary>
    /// IUPAC &amp; CRC Handbook of Chemistry and Physics (97th Edition) Standard Reduction Potentials E° at 298.15 K (V vs SHE).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, double> StandardReductionPotentials = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        ["Li(+)/Li"] = -3.040,
        ["K(+)/K"] = -2.931,
        ["Ca(2+)/Ca"] = -2.868,
        ["Na(+)/Na"] = -2.710,
        ["Mg(2+)/Mg"] = -2.372,
        ["Al(3+)/Al"] = -1.662,
        ["Mn(2+)/Mn"] = -1.185,
        ["Zn(2+)/Zn"] = -0.763,
        ["Cr(3+)/Cr"] = -0.744,
        ["Fe(2+)/Fe"] = -0.440,
        ["Cd(2+)/Cd"] = -0.403,
        ["Co(2+)/Co"] = -0.280,
        ["Ni(2+)/Ni"] = -0.257,
        ["Sn(2+)/Sn"] = -0.136,
        ["Pb(2+)/Pb"] = -0.126,
        ["2H(+)/H2"] = 0.000, // Standard Hydrogen Electrode reference
        ["Cu(2+)/Cu"] = +0.340,
        ["I2/2I(-)"] = +0.5355,
        ["Fe(3+)/Fe(2+)"] = +0.771,
        ["Ag(+)/Ag"] = +0.7996,
        ["Hg2(2+)/2Hg"] = +0.7973,
        ["Br2/2Br(-)"] = +1.066,
        ["O2+4H(+)/2H2O"] = +1.229,
        ["Cr2O7(2-)+14H(+)/2Cr(3+)"] = +1.330,
        ["Cl2/2Cl(-)"] = +1.358,
        ["PbO2+4H(+)+SO4(2-)/PbSO4"] = +1.685,
        ["MnO4(-)+8H(+)/Mn(2+)"] = +1.507,
        ["H2O2+2H(+)/2H2O"] = +1.776,
        ["F2/2F(-)"] = +2.870
    };

    /// <summary>
    /// Retrieves the standard reduction potential E° (V vs SHE) for a given redox half-reaction couple.
    /// </summary>
    public static double GetStandardReductionPotential(string halfReactionCouple)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(halfReactionCouple);
        if (StandardReductionPotentials.TryGetValue(halfReactionCouple.Trim(), out double potential))
        {
            return potential;
        }
        throw new KeyNotFoundException($"Standard reduction potential for redox couple '{halfReactionCouple}' not found in database.");
    }

    /// <summary>
    /// Calculates the standard galvanic cell potential E°_cell = E°(cathode) - E°(anode).
    /// </summary>
    public static double CalculateStandardCellPotential(string cathodeCouple, string anodeCouple)
    {
        double eCathode = GetStandardReductionPotential(cathodeCouple);
        double eAnode = GetStandardReductionPotential(anodeCouple);
        return eCathode - eAnode;
    }

    /// <summary>
    /// Calculates the non-standard electromotive cell potential (E_cell) via the Nernst equation:
    /// E = E° - (RT / nF) * ln(Q).
    /// </summary>
    /// <param name="standardCellPotentialVolts">Standard cell potential E° (V).</param>
    /// <param name="electronsTransferred">Moles of electrons transferred (n).</param>
    /// <param name="reactionQuotientQ">Reaction quotient Q = [Products]^p / [Reactants]^r.</param>
    /// <param name="temperatureKelvin">Absolute temperature in Kelvin (default: 298.15 K).</param>
    /// <returns>NernstResult record.</returns>
    public static NernstResult CalculateNernstPotential(
        double standardCellPotentialVolts,
        int electronsTransferred,
        double reactionQuotientQ,
        double temperatureKelvin = 298.15
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(electronsTransferred);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reactionQuotientQ);
        ArgumentOutOfRangeException.ThrowIfNegative(temperatureKelvin);

        double nernstTerm = (GasConstantR * temperatureKelvin / (electronsTransferred * FaradayConstantF)) * Math.Log(reactionQuotientQ);
        double eCell = standardCellPotentialVolts - nernstTerm;

        return new NernstResult(
            eCell,
            standardCellPotentialVolts,
            electronsTransferred,
            reactionQuotientQ,
            temperatureKelvin,
            IsSpontaneousGalvanic: eCell > 0
        );
    }
}
