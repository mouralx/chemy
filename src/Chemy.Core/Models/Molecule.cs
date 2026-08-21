using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Chemy.Core.Parsing;
using Chemy.Core.Quantum;
using Chemy.Core.Rendering;
using Chemy.Core.Spatial;
using Chemy.Core.Structure;

namespace Chemy.Core;

/// <summary>
/// Immutable molecular entity encapsulating an atomic graph, covalent bonding topology,
/// Hill-system empirical formula, molar mass, and net electrical charge.
/// </summary>
public record Molecule
{
    /// <summary>Common or IUPAC name of the molecule.</summary>
    public string Name { get; init; }

    /// <summary>Immutable list of constituent atoms comprising the molecule.</summary>
    public ImmutableList<Atom> Atoms { get; init; }

    /// <summary>Immutable list of covalent/ionic/aromatic bonds connecting atom indices.</summary>
    public ImmutableList<Bond> Bonds { get; init; }

    /// <summary>Molar mass calculated from standard IUPAC atomic weights (in g/mol or u).</summary>
    public double MolecularWeight => Atoms.Sum(a => a.Element.StandardAtomicMass);

    /// <summary>Net electrostatic charge computed from the sum of atomic net charges.</summary>
    public int NetCharge => Atoms.Sum(a => a.NetCharge);

    /// <summary>
    /// Indicates whether the molecule possesses explicit covalent/aromatic bond topology
    /// (e.g. parsed from SMILES or Molfile) as opposed to a composition-only empirical formula.
    /// </summary>
    public bool HasBondedTopology => Bonds.Count > 0 || Atoms.Count <= 1;

    /// <summary>
    /// Constructs a Molecule instance with validated bonding graph topology.
    /// </summary>
    /// <param name="name">Descriptive name.</param>
    /// <param name="atoms">Collection of constituent atoms.</param>
    /// <param name="bonds">Optional collection of chemical bonds between atoms.</param>
    public Molecule(string name, IEnumerable<Atom> atoms, IEnumerable<Bond>? bonds = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(atoms);

        Name = name;
        Atoms = atoms.ToImmutableList();
        Bonds = (bonds ?? Enumerable.Empty<Bond>()).ToImmutableList();

        ValidateBonds();
    }

    /// <summary>
    /// Ensures that all bond indices map to valid atoms and no self-loops exist.
    /// </summary>
    private void ValidateBonds()
    {
        int count = Atoms.Count;
        foreach (var bond in Bonds)
        {
            if (bond.Atom1Index < 0 || bond.Atom1Index >= count || bond.Atom2Index < 0 || bond.Atom2Index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(Bonds), $"Bond indices ({bond.Atom1Index}, {bond.Atom2Index}) are out of range for atom count {count}.");
            }

            if (bond.Atom1Index == bond.Atom2Index)
            {
                throw new ArgumentException($"Self-referencing bond at atom index {bond.Atom1Index} is invalid.");
            }
        }
    }

    /// <summary>
    /// Formats the empirical chemical formula following the IUPAC Hill system
    /// (Carbon first, Hydrogen second, followed by all remaining elements in alphabetical order).
    /// </summary>
    public string ChemicalFormula
    {
        get
        {
            var counts = Atoms
                .GroupBy(a => a.Element.Symbol)
                .ToDictionary(g => g.Key, g => g.Count());

            var builder = new StringBuilder();

            // Hill system: C first, then H
            if (counts.Remove("C", out int cCount))
            {
                AppendElement(builder, "C", cCount);
                if (counts.Remove("H", out int hCount))
                {
                    AppendElement(builder, "H", hCount);
                }
            }

            // Remaining elements in alphabetical order
            foreach (var key in counts.Keys.OrderBy(k => k))
            {
                AppendElement(builder, key, counts[key]);
            }

            // Append formal ionic charge if non-neutral
            int charge = NetCharge;
            if (charge != 0)
            {
                builder.Append(charge switch
                {
                    1 => "+",
                    -1 => "-",
                    > 0 => $"+{charge}",
                    _ => $"{charge}"
                });
            }

            return builder.ToString();

            static void AppendElement(StringBuilder sb, string symbol, int count)
            {
                sb.Append(symbol);
                if (count > 1) sb.Append(count);
            }
        }
    }

    /// <summary>Predefined standard Water (H2O) molecule.</summary>
    public static Molecule Water => CreateWater();

    /// <summary>Predefined standard Carbon Dioxide (CO2) molecule.</summary>
    public static Molecule CarbonDioxide => CreateCarbonDioxide();

    /// <summary>Predefined standard Methane (CH4) molecule.</summary>
    public static Molecule Methane => CreateMethane();

    /// <summary>Parses a chemical formula string into a Molecule instance.</summary>
    public static Molecule Parse(string formula, string? name = null) => FormulaParser.Parse(formula, name);

    /// <summary>Parses an organic SMILES string into a Molecule instance with implicit hydrogen graph expansion.</summary>
    public static Molecule FromSmiles(string smiles, string? name = null) => SmilesParser.Parse(smiles, name);

    /// <summary>Attempts to parse an organic SMILES string into a Molecule instance without throwing an exception.</summary>
    public static bool TryParseSmiles(string smiles, [NotNullWhen(true)] out Molecule? result) => SmilesParser.TryParse(smiles, out result);

    /// <summary>Attempts to parse an organic SMILES string with an explicit name.</summary>
    public static bool TryParseSmiles(string smiles, string? name, [NotNullWhen(true)] out Molecule? result) => SmilesParser.TryParse(smiles, name, out result, out _);

    /// <summary>Attempts to parse an organic SMILES string, returning error messages if invalid.</summary>
    public static bool TryParseSmiles(string smiles, string? name, [NotNullWhen(true)] out Molecule? result, out string? errorMessage) => SmilesParser.TryParse(smiles, name, out result, out errorMessage);

    /// <summary>Attempts to parse a chemical formula string without throwing an exception.</summary>
    public static bool TryParse(string formula, [NotNullWhen(true)] out Molecule? result) => FormulaParser.TryParse(formula, out result);

    /// <summary>Attempts to parse a chemical formula string with an explicit name.</summary>
    public static bool TryParse(string formula, string? name, [NotNullWhen(true)] out Molecule? result) => FormulaParser.TryParse(formula, name, out result);

    /// <summary>Attempts to parse a chemical formula string, returning specific syntax error messages if invalid.</summary>
    public static bool TryParse(string formula, string? name, [NotNullWhen(true)] out Molecule? result, out string? errorMessage) => FormulaParser.TryParse(formula, name, out result, out errorMessage);


    /// <summary>Detects organic functional groups present in the molecular graph.</summary>
    public IReadOnlySet<FunctionalGroup> GetFunctionalGroups() => FunctionalGroupDetector.Detect(this);

    /// <summary>Generates 3D Cartesian coordinates and VSEPR spatial geometry.</summary>
    public Molecule3D To3D(string? overrideShape = null) => Geometry3DEngine.Generate3D(this, overrideShape);

    /// <summary>Generates planar 2D Cartesian coordinates embedded in 3D Euclidean space (Z = 0.0).</summary>
    public Molecule3D ToPlanar3D() => Geometry3DEngine.GeneratePlanar3D(this);

    /// <summary>Renders a resolution-independent vector SVG diagram card.</summary>
    public string ToSvg(bool isDarkMode = true) => SvgRenderer.RenderMoleculeSvg(this, isDarkMode);

    /// <summary>Renders standard IUPAC / ChemDraw 2D skeletal line structural diagram in vector SVG.</summary>
    public string ToSkeletalSvg(bool isDarkMode = true, int width = 600, int height = 400) => SkeletalSvgRenderer.Render(this, isDarkMode, width, height);

    /// <summary>Computes Hückel Molecular Orbital (HMO) electronic structure, HOMO/LUMO bandgaps, and resonance energy.</summary>
    public HuckelResult ComputeHuckelOrbitals(double betaEv = HuckelEngine.DefaultBetaEv) => HuckelEngine.Analyze(this, betaEv);

    /// <summary>Saves a vector SVG diagram card directly to a local disk path.</summary>
    public void SaveSvg(string filePath, bool isDarkMode = true) => File.WriteAllText(filePath, ToSvg(isDarkMode));

    private static Molecule CreateWater()
    {
        var h1 = new Atom(Elements.Hydrogen, 0);
        var h2 = new Atom(Elements.Hydrogen, 0);
        var o = new Atom(Elements.Oxygen, 8);

        return new Molecule("Water", [o, h1, h2], [new Bond(0, 1), new Bond(0, 2)]);
    }

    private static Molecule CreateCarbonDioxide()
    {
        var c = new Atom(Elements.Carbon, 6);
        var o1 = new Atom(Elements.Oxygen, 8);
        var o2 = new Atom(Elements.Oxygen, 8);

        return new Molecule("Carbon Dioxide", [c, o1, o2], [new Bond(0, 1, BondType.Double), new Bond(0, 2, BondType.Double)]);
    }

    private static Molecule CreateMethane()
    {
        var c = new Atom(Elements.Carbon, 6);
        var h1 = new Atom(Elements.Hydrogen, 0);
        var h2 = new Atom(Elements.Hydrogen, 0);
        var h3 = new Atom(Elements.Hydrogen, 0);
        var h4 = new Atom(Elements.Hydrogen, 0);

        return new Molecule("Methane", [c, h1, h2, h3, h4], [new Bond(0, 1), new Bond(0, 2), new Bond(0, 3), new Bond(0, 4)]);
    }

    /// <summary>Formats the molecule for console output.</summary>
    public override string ToString() => $"{Name} ({ChemicalFormula}, {MolecularWeight:F3} g/mol)";
}