using System.Collections.Frozen;

namespace Chemy.Core.Thermodynamics;

public record StandardThermodynamicProperties(
    double EnthalpyOfFormationkJPerMol,
    double MolarEntropyJPerMolK,
    double GibbsFreeEnergykJPerMol
);

public static class ThermodynamicData
{
    private static readonly Dictionary<string, StandardThermodynamicProperties> Database = new(StringComparer.OrdinalIgnoreCase)
    {
        { "H2", new(0.0, 130.7, 0.0) },
        { "O2", new(0.0, 205.2, 0.0) },
        { "N2", new(0.0, 191.6, 0.0) },
        { "C", new(0.0, 5.74, 0.0) },
        { "Fe", new(0.0, 27.3, 0.0) },
        { "Cu", new(0.0, 33.15, 0.0) },
        { "Na", new(0.0, 51.3, 0.0) },
        { "Cl2", new(0.0, 223.1, 0.0) },
        { "H2O", new(-285.8, 69.91, -237.1) },
        { "CO2", new(-393.5, 213.8, -394.4) },
        { "CO", new(-110.5, 197.7, -137.2) },
        { "CH4", new(-74.6, 186.3, -50.5) },
        { "C2H5OH", new(-277.7, 160.7, -174.8) },
        { "C6H12O6", new(-1273.3, 212.1, -910.0) },
        { "NH3", new(-45.9, 192.8, -16.4) },
        { "Fe2O3", new(-824.2, 87.4, -742.2) },
        { "NaCl", new(-411.2, 72.1, -384.1) },
        { "NaOH", new(-425.6, 64.5, -379.5) },
        { "HCl", new(-92.3, 186.9, -95.3) },
        { "CuSO4", new(-771.4, 109.2, -662.2) },
        { "Na2SO4", new(-1387.1, 149.6, -1270.2) },
        { "Cu(OH)2", new(-449.8, 108.0, -359.8) }
    };

    private static readonly FrozenDictionary<string, StandardThermodynamicProperties> Lookup = Database.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static bool TryGetProperties(string formula, out StandardThermodynamicProperties properties) =>
        Lookup.TryGetValue(formula, out properties!);
}
