namespace Chemy.Core.Thermodynamics;

using Chemy.Core.Scientific;

/// <summary>
/// One published NIST Shomate coefficient interval. Intervals are interpreted as
/// half-open [TMin, TMax), except the final interval for a species, which includes TMax.
/// </summary>
public sealed record ShomateCoefficients(
    double A, double B, double C, double D, double E, double F, double G, double H,
    double TMinKelvin, double TMaxKelvin
);

/// <summary>A temperature interval supported by a published Shomate coefficient set.</summary>
public sealed record ShomateTemperatureRange(double MinimumKelvin, double MaximumKelvin);

/// <summary>Thermodynamic state calculated from one explicitly selected Shomate interval.</summary>
/// <param name="TemperatureKelvin">Absolute temperature in kelvin.</param>
/// <param name="Phase">Physical state represented by the source record.</param>
/// <param name="HeatCapacityCp">Constant-pressure molar heat capacity in J/(mol·K).</param>
/// <param name="StandardEnthalpyH">Standard molar formation enthalpy at T in kJ/mol.</param>
/// <param name="StandardEntropyS">Standard molar entropy in J/(mol·K).</param>
/// <param name="StandardGibbsFreeEnergyG">Standard molar formation Gibbs energy at T in kJ/mol.</param>
/// <param name="CoefficientRange">Published range of the selected coefficient segment.</param>
/// <param name="SourceUrl">NIST Chemistry WebBook species page used for the coefficient table.</param>
/// <param name="MethodInfo">Scientific method provenance and applicability metadata.</param>
public sealed record ShomateThermoResult(
    double TemperatureKelvin,
    string Phase,
    double HeatCapacityCp,
    double StandardEnthalpyH,
    double StandardEntropyS,
    double StandardGibbsFreeEnergyG,
    ShomateTemperatureRange CoefficientRange,
    string SourceUrl,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// Piecewise NIST Chemistry WebBook Shomate thermodynamics for a bounded set of gas species.
/// Values outside a species' published intervals are rejected; no extrapolation is performed.
/// </summary>
public static class ShomateThermodynamics
{
    private sealed record SpeciesDefinition(string Phase, string SourceUrl, ShomateCoefficients[] Segments);

    private static readonly ScientificMethodInfo ShomateMethodInfo = new(
        "NIST Chemistry WebBook / JANAF Piecewise Shomate Thermodynamics",
        "1998.2",
        EvidenceLevel.ExactEquation,
        "Listed gas species only, within the published temperature interval selected for that species.",
        [
            "Ideal-gas and standard-state assumptions from the source tables.",
            "No extrapolation outside published intervals; the selected interval and source URL are returned with every result."
        ]
    );

    // NIST Chemistry WebBook SRD 69, Gas Phase Heat Capacity (Shomate Equation).
    private static readonly Dictionary<string, SpeciesDefinition> Database = new(StringComparer.OrdinalIgnoreCase)
    {
        ["H2O(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C7732185&Table=on&Type=JANAFG", [
            new(30.09200, 6.832514, 6.793435, -2.534480, 0.082139, -250.8810, 223.3967, -241.8264, 500.0, 1700.0),
            new(41.96426, 8.622053, -1.499780, 0.098119, -11.15764, -272.1797, 219.7809, -241.8264, 1700.0, 6000.0)
        ]),
        ["CO2(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C124389&Table=on&Type=JANAFG", [
            new(24.99735, 55.18696, -33.69137, 7.948387, -0.136638, -403.6075, 228.2431, -393.5224, 298.0, 1200.0),
            new(58.16639, 2.720074, -0.492289, 0.038844, -6.447293, -425.9186, 263.6125, -393.5224, 1200.0, 6000.0)
        ]),
        ["CO(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C630080&Table=on&Type=JANAFG", [
            new(25.56759, 6.096130, 4.054656, -2.671301, 0.131021, -118.0089, 227.3665, -110.5271, 298.0, 1300.0),
            new(35.15070, 1.300095, -0.205921, 0.013550, -3.282780, -127.8375, 231.7120, -110.5271, 1300.0, 6000.0)
        ]),
        ["CH4(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C74828&Table=on&Type=JANAFG", [
            new(-0.703029, 108.4773, -42.52157, 5.862788, 0.678565, -76.84376, 158.7163, -74.87310, 298.0, 1300.0),
            new(85.81217, 11.26467, -2.114146, 0.138190, -26.42221, -153.5327, 224.4143, -74.87310, 1300.0, 6000.0)
        ]),
        ["O2(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C7782447&Table=on&Type=JANAFG", [
            new(31.32234, -20.23531, 57.86644, -36.50624, -0.007374, -8.903471, 246.7945, 0.0, 100.0, 700.0),
            new(30.03235, 8.772972, -3.988133, 0.788313, -0.741599, -11.32468, 236.1663, 0.0, 700.0, 2000.0),
            new(20.91111, 10.72071, -2.020498, 0.146449, 9.245722, 5.337651, 237.6185, 0.0, 2000.0, 6000.0)
        ]),
        ["N2(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C7727379&Table=on&Type=JANAFG", [
            new(28.98641, 1.853978, -9.647459, 16.63537, 0.000117, -8.671914, 226.4168, 0.0, 100.0, 500.0),
            new(19.50583, 19.88705, -8.598535, 1.369784, 0.527601, -4.935202, 212.3900, 0.0, 500.0, 2000.0),
            new(35.51872, 1.128728, -0.196103, 0.014662, -4.553760, -18.97091, 224.9810, 0.0, 2000.0, 6000.0)
        ]),
        ["H2(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C1333740&Table=on&Type=JANAFG", [
            new(33.066178, -11.363417, 11.432816, -2.772874, -0.158558, -9.980797, 172.707974, 0.0, 298.0, 1000.0),
            new(18.563083, 12.257357, -2.859786, 0.268238, 1.977990, -1.147438, 156.288133, 0.0, 1000.0, 2500.0),
            new(43.413560, -4.293079, 1.272428, -0.096876, -20.533862, -38.515158, 162.081354, 0.0, 2500.0, 6000.0)
        ]),
        ["NH3(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C7664417&Table=on&Type=JANAFG", [
            new(19.99563, 49.77119, -15.37599, 1.921168, 0.189174, -53.30667, 203.8591, -45.89806, 298.0, 1400.0),
            new(52.02427, 18.48801, -3.765128, 0.248541, -12.45799, -85.53895, 223.8022, -45.89806, 1400.0, 6000.0)
        ]),
        ["C2H4(g)"] = new("Gas", "https://webbook.nist.gov/cgi/cbook.cgi?ID=C74851&Table=on&Type=JANAFG", [
            new(-6.387880, 184.4019, -112.9718, 28.49593, 0.315540, 48.17332, 163.1568, 52.46694, 298.0, 1200.0),
            new(106.5104, 13.73260, -2.628481, 0.174595, -26.14469, -35.36237, 275.0424, 52.46694, 1200.0, 6000.0)
        ])
    };

    /// <summary>Returns the species keys supported by the embedded NIST dataset.</summary>
    public static IReadOnlyCollection<string> SupportedSpecies => Database.Keys;

    /// <summary>Returns the published coefficient intervals for a supported species.</summary>
    public static IReadOnlyList<ShomateTemperatureRange> GetSupportedTemperatureRanges(string speciesKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(speciesKey);
        if (!Database.TryGetValue(speciesKey, out var species))
        {
            return [];
        }

        return species.Segments
            .Select(segment => new ShomateTemperatureRange(segment.TMinKelvin, segment.TMaxKelvin))
            .ToArray();
    }

    /// <summary>
    /// Evaluates thermodynamic state at a specified temperature. Returns <see langword="null"/>
    /// only when the species key is unsupported; invalid temperatures throw.
    /// </summary>
    public static ShomateThermoResult? Evaluate(string speciesKey, double temperatureKelvin = 298.15)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(speciesKey);
        if (!double.IsFinite(temperatureKelvin) || temperatureKelvin <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(temperatureKelvin), temperatureKelvin, "Temperature must be a finite positive value in kelvin.");
        }

        if (!Database.TryGetValue(speciesKey, out var species))
        {
            return null;
        }

        ShomateCoefficients? coefficients = null;
        for (int index = 0; index < species.Segments.Length; index++)
        {
            var candidate = species.Segments[index];
            bool isFinal = index == species.Segments.Length - 1;
            if (temperatureKelvin >= candidate.TMinKelvin &&
                (temperatureKelvin < candidate.TMaxKelvin || (isFinal && temperatureKelvin <= candidate.TMaxKelvin)))
            {
                coefficients = candidate;
                break;
            }
        }

        if (coefficients is null)
        {
            string intervals = string.Join(", ", species.Segments.Select(p => $"[{p.TMinKelvin}, {p.TMaxKelvin}] K"));
            throw new ArgumentOutOfRangeException(
                nameof(temperatureKelvin),
                temperatureKelvin,
                $"Temperature is outside the published NIST Shomate interval(s) for {speciesKey}: {intervals}. Extrapolation is not permitted.");
        }

        var p = coefficients;
        double t = temperatureKelvin / 1000.0;
        double cp = p.A + p.B * t + p.C * t * t + p.D * t * t * t + p.E / (t * t);

        // NIST publishes H° - H°298.15 = polynomial + F - H. Because H is the
        // standard formation enthalpy at 298.15 K, polynomial + F is ΔfH°(T).
        double formationEnthalpy = p.A * t + (p.B * t * t) / 2.0 + (p.C * t * t * t) / 3.0
            + (p.D * t * t * t * t) / 4.0 - (p.E / t) + p.F;
        double entropy = p.A * Math.Log(t) + p.B * t + (p.C * t * t) / 2.0
            + (p.D * t * t * t) / 3.0 - (p.E / (2.0 * t * t)) + p.G;
        double formationGibbsEnergy = formationEnthalpy - temperatureKelvin * entropy / 1000.0;

        return new ShomateThermoResult(
            temperatureKelvin,
            species.Phase,
            cp,
            formationEnthalpy,
            entropy,
            formationGibbsEnergy,
            new ShomateTemperatureRange(p.TMinKelvin, p.TMaxKelvin),
            species.SourceUrl,
            ShomateMethodInfo
        );
    }
}
