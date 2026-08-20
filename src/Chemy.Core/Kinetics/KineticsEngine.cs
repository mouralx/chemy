namespace Chemy.Core.Kinetics;

/// <summary>
/// Encapsulates reaction half-life calculation results.
/// </summary>
/// <param name="HalfLifeTime">Time required for reactant concentration to decrease by 50% (seconds).</param>
/// <param name="ReactionOrder">Reaction order (0, 1, or 2).</param>
/// <param name="RateConstantK">Reaction rate constant k.</param>
/// <param name="InitialConcentrationMolar">Initial reactant concentration [A]₀ in Molar (M).</param>
public record HalfLifeResult(
    double HalfLifeTime,
    int ReactionOrder,
    double RateConstantK,
    double InitialConcentrationMolar
)
{
    /// <summary>Formats the half-life result as a string.</summary>
    public override string ToString() => $"t_1/2 = {HalfLifeTime:F3} s (Order = {ReactionOrder}, k = {RateConstantK})";
}

/// <summary>
/// Encapsulates Arrhenius rate constant calculation results.
/// </summary>
/// <param name="RateConstantK">Calculated rate constant k.</param>
/// <param name="PreExponentialFactorA">Frequency/pre-exponential factor A.</param>
/// <param name="ActivationEnergykJPerMol">Activation energy Ea in kJ/mol.</param>
/// <param name="TemperatureKelvin">Temperature in Kelvin (K).</param>
public record ArrheniusResult(
    double RateConstantK,
    double PreExponentialFactorA,
    double ActivationEnergykJPerMol,
    double TemperatureKelvin
)
{
    /// <summary>Formats the Arrhenius calculation result as a string.</summary>
    public override string ToString() => $"k = {RateConstantK:E3} (E_a = {ActivationEnergykJPerMol:F1} kJ/mol, T = {TemperatureKelvin:F1} K)";
}

/// <summary>
/// Runge-Kutta 4th Order (RK4) Differential Numerical Solver &amp; Cascade Simulator.
/// Solves integrated rate laws (0th, 1st, 2nd order), half-lives, and Arrhenius activation energy equations.
/// </summary>
public static class KineticsEngine
{
    /// <summary>Ideal gas constant R in J/(mol·K).</summary>
    private const double GasConstantR = 8.314462618;

    /// <summary>
    /// Calculates the half-life (t_1/2) for a given reaction order and rate constant.
    /// </summary>
    /// <param name="order">Reaction order (0 for [A]0/2k, 1 for ln(2)/k, 2 for 1/(k[A]0)).</param>
    /// <param name="rateConstantK">Rate constant k (units depend on reaction order).</param>
    /// <param name="initialConcentrationMolar">Initial concentration [A]0 (M).</param>
    /// <returns>HalfLifeResult record.</returns>
    public static HalfLifeResult CalculateHalfLife(int order, double rateConstantK, double initialConcentrationMolar = 1.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rateConstantK);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialConcentrationMolar);

        double halfLife = order switch
        {
            0 => initialConcentrationMolar / (2.0 * rateConstantK),
            1 => Math.Log(2.0) / rateConstantK,
            2 => 1.0 / (rateConstantK * initialConcentrationMolar),
            _ => throw new ArgumentOutOfRangeException(nameof(order), "Reaction order must be 0, 1, or 2.")
        };

        return new HalfLifeResult(halfLife, order, rateConstantK, initialConcentrationMolar);
    }

    /// <summary>
    /// Calculates the Arrhenius rate constant k = A * exp(-Ea / RT).
    /// </summary>
    /// <param name="preExponentialFactorA">Pre-exponential collision frequency factor A.</param>
    /// <param name="activationEnergykJPerMol">Activation energy Ea in kJ/mol.</param>
    /// <param name="temperatureKelvin">Absolute temperature in Kelvin (K).</param>
    /// <returns>ArrheniusResult record.</returns>
    public static ArrheniusResult CalculateRateConstant(double preExponentialFactorA, double activationEnergykJPerMol, double temperatureKelvin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(preExponentialFactorA);
        ArgumentOutOfRangeException.ThrowIfNegative(activationEnergykJPerMol);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(temperatureKelvin);

        double eaJoules = activationEnergykJPerMol * 1000.0;
        double k = preExponentialFactorA * Math.Exp(-eaJoules / (GasConstantR * temperatureKelvin));

        return new ArrheniusResult(k, preExponentialFactorA, activationEnergykJPerMol, temperatureKelvin);
    }

    /// <summary>
    /// Calculates the activation energy (Ea) from rate constants measured at two distinct temperatures.
    /// Formula: Ea = R * (T1 * T2 / (T2 - T1)) * ln(k2 / k1)
    /// </summary>
    public static double CalculateActivationEnergy(double k1, double temperatureKelvin1, double k2, double temperatureKelvin2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(temperatureKelvin1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(temperatureKelvin2);

        if (Math.Abs(temperatureKelvin1 - temperatureKelvin2) < 0.001)
        {
            throw new ArgumentException("Temperatures must be distinct.");
        }

        double eaJoules = (GasConstantR * temperatureKelvin1 * temperatureKelvin2 / (temperatureKelvin2 - temperatureKelvin1)) * Math.Log(k2 / k1);
        return eaJoules / 1000.0;
    }
}
