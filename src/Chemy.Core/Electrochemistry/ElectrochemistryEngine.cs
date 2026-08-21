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
