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
        { "Al", new(0.0, 28.3, 0.0) },
        { "Zn", new(0.0, 41.6, 0.0) },
        { "Na", new(0.0, 51.3, 0.0) },
        { "Cl2", new(0.0, 223.1, 0.0) },
        { "Br2", new(0.0, 152.2, 0.0) },
        { "I2", new(0.0, 116.1, 0.0) },
        { "H2O", new(-285.8, 69.91, -237.1) },
        { "CO2", new(-393.5, 213.8, -394.4) },
        { "CO", new(-110.5, 197.7, -137.2) },
        { "CH4", new(-74.6, 186.3, -50.5) },
        { "C2H6", new(-84.0, 229.2, -32.0) },
        { "C3H8", new(-103.8, 270.3, -23.4) },
        { "C4H10", new(-125.6, 310.2, -15.7) },
        { "C2H4", new(52.4, 219.3, 68.4) },
        { "C2H2", new(227.4, 200.9, 209.9) },
        { "C6H6", new(49.0, 173.3, 124.3) },
        { "CH3OH", new(-239.1, 126.8, -166.3) },
        { "CH4O", new(-239.1, 126.8, -166.3) },
        { "C2H5OH", new(-277.7, 160.7, -174.8) },
        { "C2H6O", new(-277.7, 160.7, -174.8) },
        { "CH3COOH", new(-484.5, 159.8, -389.9) },
        { "C2H4O2", new(-484.5, 159.8, -389.9) },
        { "HCOOH", new(-425.1, 129.0, -361.4) },
        { "CH2O2", new(-425.1, 129.0, -361.4) },
        { "C3H6O", new(-248.4, 200.4, -155.4) },
        { "CH3COCH3", new(-248.4, 200.4, -155.4) },
        { "C6H12O6", new(-1273.3, 212.1, -910.0) },
        { "NH3", new(-45.9, 192.8, -16.4) },
        { "H3N", new(-45.9, 192.8, -16.4) },
        { "NO", new(90.3, 210.8, 86.6) },
        { "NO2", new(33.2, 240.1, 51.3) },
        { "SO2", new(-296.8, 248.2, -300.2) },
        { "SO3", new(-395.7, 256.8, -371.1) },
        { "H2SO4", new(-814.0, 156.9, -690.0) },
        { "H2O4S", new(-814.0, 156.9, -690.0) },
        { "HNO3", new(-174.1, 155.6, -80.7) },
        { "HNO3_", new(-174.1, 155.6, -80.7) },
        { "Fe2O3", new(-824.2, 87.4, -742.2) },
        { "Fe2O3_", new(-824.2, 87.4, -742.2) },
        { "Al2O3", new(-1675.7, 50.9, -1582.3) },
        { "CaO", new(-635.1, 39.9, -604.0) },
        { "CaCO3", new(-1206.9, 92.9, -1128.8) },
        { "CCaO3", new(-1206.9, 92.9, -1128.8) },
        { "NaCl", new(-411.2, 72.1, -384.1) },
        { "ClNa", new(-411.2, 72.1, -384.1) },
        { "NaOH", new(-425.6, 64.5, -379.5) },
        { "HNaO", new(-425.6, 64.5, -379.5) },
        { "HCl", new(-92.3, 186.9, -95.3) },
        { "ClH", new(-92.3, 186.9, -95.3) },
        { "CuSO4", new(-771.4, 109.2, -662.2) },
        { "CuO4S", new(-771.4, 109.2, -662.2) },
        { "Na2SO4", new(-1387.1, 149.6, -1270.2) },
        { "Na2O4S", new(-1387.1, 149.6, -1270.2) },
        { "Cu(OH)2", new(-449.8, 108.0, -359.8) },
        { "CuH2O2", new(-449.8, 108.0, -359.8) }
    };

    private static readonly FrozenDictionary<string, StandardThermodynamicProperties> Lookup = Database.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static bool TryGetProperties(string formula, out StandardThermodynamicProperties properties) =>
        Lookup.TryGetValue(formula, out properties!);
}
