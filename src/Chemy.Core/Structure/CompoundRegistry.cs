using System.Diagnostics.CodeAnalysis;

namespace Chemy.Core.Structure;

/// <summary>
/// Curated registry of canonical SMILES topologies for common molecular compounds,
/// biochemicals, pharmaceutical APIs, and industrial polymers.
/// Allows name and empirical formula lookups to automatically construct authentic organic graphs.
/// </summary>
public static class CompoundRegistry
{
    private static readonly Dictionary<string, (string CanonicalName, string Smiles)> Registry = new(StringComparer.OrdinalIgnoreCase)
    {
        // Solvents & Organics
        ["H2O"] = ("Water", "O"),
        ["Water"] = ("Water", "O"),
        ["CH4"] = ("Methane", "C"),
        ["Methane"] = ("Methane", "C"),
        ["CO2"] = ("Carbon Dioxide", "O=C=O"),
        ["Carbon Dioxide"] = ("Carbon Dioxide", "O=C=O"),
        ["CCO"] = ("Ethanol", "CCO"),
        ["C2H6O"] = ("Ethanol", "CCO"),
        ["Ethanol"] = ("Ethanol", "CCO"),
        ["CC(=O)C"] = ("Acetone", "CC(=O)C"),
        ["C3H6O"] = ("Acetone", "CC(=O)C"),
        ["Acetone"] = ("Acetone", "CC(=O)C"),
        ["c1ccccc1"] = ("Benzene", "c1ccccc1"),
        ["C6H6"] = ("Benzene", "c1ccccc1"),
        ["Benzene"] = ("Benzene", "c1ccccc1"),

        // Pharmaceuticals & Bioactives
        ["Aspirin"] = ("Aspirin", "CC(=O)Oc1ccccc1C(=O)O"),
        ["C9H8O4"] = ("Aspirin", "CC(=O)Oc1ccccc1C(=O)O"),
        ["Caffeine"] = ("Caffeine", "CN1C=NC2=C1C(=O)N(C(=O)N2C)C"),
        ["C8H10N4O2"] = ("Caffeine", "CN1C=NC2=C1C(=O)N(C(=O)N2C)C"),
        ["Paracetamol"] = ("Paracetamol", "CC(=O)Nc1ccc(O)cc1"),
        ["Acetaminophen"] = ("Paracetamol", "CC(=O)Nc1ccc(O)cc1"),
        ["C8H9NO2"] = ("Paracetamol", "CC(=O)Nc1ccc(O)cc1"),
        ["Ibuprofen"] = ("Ibuprofen", "CC(C)Cc1ccc(cc1)C(C)C(=O)O"),
        ["C13H18O2"] = ("Ibuprofen", "CC(C)Cc1ccc(cc1)C(C)C(=O)O"),
        ["Cocaine"] = ("Cocaine", "CN1C2CCC1C(C(=O)OC)C(OC(=O)c1ccccc1)C2"),
        ["C17H21NO4"] = ("Cocaine", "CN1C2CCC1C(C(=O)OC)C(OC(=O)c1ccccc1)C2"),
        ["Morphine"] = ("Morphine", "CN1CCC23C4C1CC5=C2C(=C(C=C5)O)OC3C(C=C4)O"),
        ["C17H19NO3"] = ("Morphine", "CN1CCC23C4C1CC5=C2C(=C(C=C5)O)OC3C(C=C4)O"),
        ["Nicotine"] = ("Nicotine", "CN1CCCC1c1cccnc1"),
        ["C10H14N2"] = ("Nicotine", "CN1CCCC1c1cccnc1"),

        // Biomolecules & Energy
        ["Glucose"] = ("D-Glucose", "OCC1OC(O)C(O)C(O)C1O"),
        ["C6H12O6"] = ("D-Glucose", "OCC1OC(O)C(O)C(O)C1O"),
        ["ATP"] = ("Adenosine Triphosphate", "c1nc(c2c(n1)n(cn2)C3C(C(C(O3)COP(=O)(O)OP(=O)(O)OP(=O)(O)O)O)O)N"),
        ["C10H16N5O13P3"] = ("Adenosine Triphosphate", "c1nc(c2c(n1)n(cn2)C3C(C(C(O3)COP(=O)(O)OP(=O)(O)OP(=O)(O)O)O)O)N"),

        // PFAS & Toxins
        ["PFOA"] = ("Perfluorooctanoic Acid (PFOA)", "OC(=O)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)F"),
        ["C8HF15O2"] = ("Perfluorooctanoic Acid (PFOA)", "OC(=O)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)F"),
        ["PFOS"] = ("Perfluorooctanesulfonic Acid (PFOS)", "OS(=O)(=O)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)F"),
        ["C8HF17O3S"] = ("Perfluorooctanesulfonic Acid (PFOS)", "OS(=O)(=O)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)F"),

        // Polymers & Inorganics
        ["PET"] = ("BHET Monomer", "c1cc(ccc1C(=O)OCCO)C(=O)OCCO"),
        ["PET Monomer"] = ("BHET Monomer", "c1cc(ccc1C(=O)OCCO)C(=O)OCCO"),
        ["C10H8O4"] = ("BHET Monomer", "c1cc(ccc1C(=O)OCCO)C(=O)OCCO"),
        ["H2SO4"] = ("Sulfuric Acid", "OS(=O)(=O)O"),
        ["Sulfuric Acid"] = ("Sulfuric Acid", "OS(=O)(=O)O"),
        ["NH3"] = ("Ammonia", "N"),
        ["Ammonia"] = ("Ammonia", "N")
    };

    /// <summary>
    /// Attempts to resolve a chemical name, abbreviation, or empirical formula to a canonical SMILES string.
    /// </summary>
    public static bool TryResolve(string input, [NotNullWhen(true)] out string? canonicalName, [NotNullWhen(true)] out string? canonicalSmiles)
    {
        canonicalName = null;
        canonicalSmiles = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string trimmed = input.Trim();
        if (Registry.TryGetValue(trimmed, out var entry))
        {
            canonicalName = entry.CanonicalName;
            canonicalSmiles = entry.Smiles;
            return true;
        }

        // Try stripping parentheticals, e.g. "PFOA (PFAS)" -> "PFOA"
        int parenIdx = trimmed.IndexOf('(');
        if (parenIdx > 0)
        {
            string clean = trimmed[..parenIdx].Trim();
            if (Registry.TryGetValue(clean, out entry))
            {
                canonicalName = entry.CanonicalName;
                canonicalSmiles = entry.Smiles;
                return true;
            }
        }

        return false;
    }
}
