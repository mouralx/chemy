namespace Chemy.Core.Thermodynamics;

using Chemy.Core.Scientific;

/// <summary>
/// NIST Shomate polynomial coefficients for temperature-dependent thermodynamic calculations.
/// Temperature range: T_min to T_max in Kelvin. t = T / 1000 K.
/// </summary>
public sealed record ShomateCoefficients(
    double A, double B, double C, double D, double E, double F, double G, double H,
    double TMinKelvin = 298.15, double TMaxKelvin = 2000.0
);

/// <summary>
/// Detailed thermodynamic state result at specified temperature T.
/// </summary>
/// <param name="TemperatureKelvin">Absolute temperature in Kelvin.</param>
/// <param name="Phase">Physical state of matter (Gas, Liquid, Solid, Aqueous).</param>
/// <param name="HeatCapacityCp">Constant-pressure molar heat capacity (J/(mol·K)).</param>
/// <param name="StandardEnthalpyH">Standard molar enthalpy H°(T) (kJ/mol).</param>
/// <param name="StandardEntropyS">Standard molar entropy S°(T) (J/(mol·K)).</param>
/// <param name="StandardGibbsFreeEnergyG">Standard molar Gibbs energy G°(T) (kJ/mol).</param>
/// <param name="MethodInfo">Scientific method provenance and metadata.</param>
public sealed record ShomateThermoResult(
    double TemperatureKelvin,
    string Phase,
    double HeatCapacityCp,
    double StandardEnthalpyH,
    double StandardEntropyS,
    double StandardGibbsFreeEnergyG,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// NIST Standard Reference Database Shomate Thermodynamics Engine.
/// Computes analytical temperature-dependent enthalpy, entropy, heat capacity, and Gibbs free energy.
/// Reference: Chase, M. W. (1998). NIST-JANAF Thermochemical Tables, 4th Edition. J. Phys. Chem. Ref. Data, Monograph 9.
/// </summary>
public static class ShomateThermodynamics
{
    private static readonly ScientificMethodInfo ShomateMethodInfo = new(
        "NIST Chemistry WebBook / JANAF Shomate Polynomial Thermodynamics",
        "1998.1",
        EvidenceLevel.ExactEquation,
        "Pure chemical substances in defined gas, liquid, or solid phases across 298.15 K to 2000 K.",
        ["Ideal gas / standard state pure substance assumptions."]
    );

    // Standard NIST Shomate Database entries
    private static readonly Dictionary<string, ShomateCoefficients> Database = new(StringComparer.OrdinalIgnoreCase)
    {
        // Water gas H2O(g)
        ["H2O(g)"] = new(30.09200, 6.832514, 6.793435, -2.534480, 0.082139, -250.8810, 223.3967, -241.8264),
        // Carbon dioxide CO2(g)
        ["CO2(g)"] = new(24.99735, 55.18696, -33.69137, 7.948387, -0.136638, -403.6075, 228.2431, -393.5224),
        // Carbon monoxide CO(g)
        ["CO(g)"] = new(25.56759, 6.096130, 4.054656, -2.671301, 0.131021, -118.0089, 227.3665, -110.5271),
        // Methane CH4(g)
        ["CH4(g)"] = new(-0.703029, 108.4773, -42.52157, 5.862788, 0.678565, -76.84376, 158.3163, -74.87310),
        // Oxygen O2(g)
        ["O2(g)"] = new(29.65900, 6.137261, -1.186521, 0.095780, -0.219663, -9.861391, 237.9480, 0.0),
        // Nitrogen N2(g)
        ["N2(g)"] = new(26.09200, 8.218801, -1.976141, 0.159274, 0.044434, -7.989230, 221.0200, 0.0),
        // Hydrogen H2(g)
        ["H2(g)"] = new(33.066178, -11.363417, 11.432816, -2.772874, -0.158558, -9.980797, 172.707974, 0.0)
    };

    /// <summary>
    /// Evaluates thermodynamic state (Cp, H, S, G) at a specified temperature T in Kelvin.
    /// </summary>
    public static ShomateThermoResult? Evaluate(string speciesKey, double temperatureKelvin = 298.15)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(speciesKey);

        if (!Database.TryGetValue(speciesKey, out var p))
        {
            return null;
        }

        double t = Math.Clamp(temperatureKelvin, p.TMinKelvin, p.TMaxKelvin) / 1000.0; // t = T / 1000 K

        // 1. Heat capacity: Cp = A + B*t + C*t^2 + D*t^3 + E/(t^2) [J/(mol*K)]
        double cp = p.A + p.B * t + p.C * t * t + p.D * t * t * t + p.E / (t * t);

        // 2. Standard Formation Enthalpy: H°(T) = A*t + B*t^2/2 + C*t^3/3 + D*t^4/4 - E/t + F [kJ/mol]
        double h = p.A * t + (p.B * t * t) / 2.0 + (p.C * t * t * t) / 3.0 + (p.D * t * t * t * t) / 4.0 - (p.E / t) + p.F;

        // 3. Entropy: S° = A*ln(t) + B*t + C*t^2/2 + D*t^3/3 - E/(2*t^2) + G [J/(mol*K)]
        double s = p.A * Math.Log(t) + p.B * t + (p.C * t * t) / 2.0 + (p.D * t * t * t) / 3.0 - (p.E / (2.0 * t * t)) + p.G;

        // 4. Gibbs Free Energy: G°(T) = H°(T) - T * S°(T) [kJ/mol]
        double g = h - (temperatureKelvin * s / 1000.0);

        string phase = speciesKey.Contains("(g)") ? "Gas" : (speciesKey.Contains("(l)") ? "Liquid" : "Solid");

        return new ShomateThermoResult(
            temperatureKelvin,
            phase,
            Math.Round(cp, 3),
            Math.Round(h, 3),
            Math.Round(s, 3),
            Math.Round(g, 3),
            ShomateMethodInfo
        );
    }
}
