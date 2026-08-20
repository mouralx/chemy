using System.Globalization;
using System.Text;

namespace Chemy.Core.Spatial;

/// <summary>
/// Immutable 3D spatial coordinate vector (X, Y, Z) in Angstroms (Å).
/// </summary>
/// <param name="X">X-axis Cartesian coordinate.</param>
/// <param name="Y">Y-axis Cartesian coordinate.</param>
/// <param name="Z">Z-axis Cartesian coordinate.</param>
public record Vector3D(double X, double Y, double Z)
{
    /// <summary>Formats the vector as (X, Y, Z).</summary>
    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4})";
}

/// <summary>
/// Wraps an Atom with its explicit 3D Cartesian position vector.
/// </summary>
/// <param name="Atom">Constituent atom.</param>
/// <param name="Position">3D spatial coordinate vector.</param>
public record Atom3D(Atom Atom, Vector3D Position);

/// <summary>
/// Represents a molecule positioned in 3D Euclidean space with VSEPR geometry classification,
/// ideal bond angles, and Cartesian coordinate exporters (.xyz and .pdb).
/// </summary>
/// <param name="Name">Molecular name.</param>
/// <param name="ChemicalFormula">Chemical formula.</param>
/// <param name="VseprShape">VSEPR geometry name (e.g. Linear, Tetrahedral, Octahedral).</param>
/// <param name="IdealBondAngleDegrees">Ideal valence bond angle in degrees (°).</param>
/// <param name="Atoms">List of atoms with assigned 3D Cartesian coordinates.</param>
/// <param name="SourceMolecule">Original 2D/topological Molecule instance.</param>
public record Molecule3D(
    string Name,
    string ChemicalFormula,
    string VseprShape,
    double IdealBondAngleDegrees,
    IReadOnlyList<Atom3D> Atoms,
    Molecule SourceMolecule
)
{
    /// <summary>
    /// Exports the 3D molecular structure in standard .xyz Cartesian coordinate format.
    /// </summary>
    /// <returns>.xyz file content as a string.</returns>
    public string ToXyz()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Atoms.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine($"{Name} ({ChemicalFormula}) - VSEPR: {VseprShape}");
        foreach (var a in Atoms)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-3} {1,10:F4} {2,10:F4} {3,10:F4}", a.Atom.Element.Symbol, a.Position.X, a.Position.Y, a.Position.Z));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Exports the 3D molecular structure in standard Protein Data Bank (.pdb) format with HETATM and CONECT records.
    /// </summary>
    /// <returns>.pdb file content as a string.</returns>
    public string ToPdb()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"HEADER    {Name.ToUpperInvariant()}");
        sb.AppendLine($"COMPND    {ChemicalFormula}");

        for (int i = 0; i < Atoms.Count; i++)
        {
            var a = Atoms[i];
            string atomName = a.Atom.Element.Symbol.Length == 1 ? $" {a.Atom.Element.Symbol}  " : $"{a.Atom.Element.Symbol,-4}";
            string elemSymbol = $"{a.Atom.Element.Symbol,2}";

            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "HETATM{0,5} {1} MOL A   1    {2,8:F3}{3,8:F3}{4,8:F3}  1.00  0.00          {5}",
                i + 1,
                atomName,
                a.Position.X,
                a.Position.Y,
                a.Position.Z,
                elemSymbol
            ));
        }

        // Add CONECT records for explicit bond connectivity
        for (int i = 0; i < Atoms.Count; i++)
        {
            var connected = SourceMolecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index + 1 : b.Atom1Index + 1)
                .Distinct()
                .ToList();

            if (connected.Count > 0)
            {
                sb.AppendLine($"CONECT{i + 1,5}{string.Join("", connected.Select(idx => $"{idx,5}"))}");
            }
        }

        sb.AppendLine("END");
        return sb.ToString();
    }
}

/// <summary>
/// 3D Spatial Geometry Engine.
/// Computes 3D Cartesian coordinates based on Valence Shell Electron Pair Repulsion (VSEPR) theory
/// and steric number calculations. Supports 8 fundamental geometric shapes: Linear, Bent, Trigonal Planar,
/// Trigonal Pyramidal, Tetrahedral, Square Planar, Trigonal Bipyramidal, and Octahedral.
/// </summary>
public static class Geometry3DEngine
{
    /// <summary>
    /// Generates 3D Cartesian coordinates for a molecule.
    /// </summary>
    /// <param name="molecule">Input molecule.</param>
    /// <param name="overrideShape">Optional VSEPR shape override (e.g. "Linear", "Tetrahedral").</param>
    /// <returns>Molecule3D with assigned atomic Cartesian positions.</returns>
    public static Molecule3D Generate3D(Molecule molecule, string? overrideShape = null)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        // Case 1: Monatomic species
        if (molecule.Atoms.Count <= 1)
        {
            var singleAtomList = molecule.Atoms.Select(a => new Atom3D(a, new Vector3D(0, 0, 0))).ToList();
            return new Molecule3D(molecule.Name, molecule.ChemicalFormula, "Monatomic", 0.0, singleAtomList, molecule);
        }

        // Case 2: Diatomic species (Linear, 180°)
        if (molecule.Atoms.Count == 2)
        {
            if (molecule.Bonds.Count == 0)
                throw new NotSupportedException("3D geometry requires an explicit bond connecting a diatomic molecule.");
            var diatomicList = new List<Atom3D>
            {
                new(molecule.Atoms[0], new Vector3D(-0.6, 0, 0)),
                new(molecule.Atoms[1], new Vector3D(0.6, 0, 0))
            };
            return new Molecule3D(molecule.Name, molecule.ChemicalFormula, "Linear", 180.0, diatomicList, molecule);
        }

        if (molecule.Bonds.Count < molecule.Atoms.Count - 1)
            throw new NotSupportedException("3D geometry requires molecular connectivity; empirical formulas do not define coordinates.");

        // Case 3: Polyatomic species (Determine central atom and coordination sphere)
        var centerAtom = molecule.Atoms.FirstOrDefault(a => a.Element.Symbol != "H") ?? molecule.Atoms[0];
        int centerIndex = molecule.Atoms.IndexOf(centerAtom);

        var outerAtoms = molecule.Atoms.Where((_, idx) => idx != centerIndex).ToList();
        int n = outerAtoms.Count;

        string shape;
        double angle;
        var positions = new List<Vector3D> { new(0, 0, 0) };

        if (!string.IsNullOrWhiteSpace(overrideShape) && overrideShape != "Auto")
        {
            shape = overrideShape;
            (angle, positions) = CalculatePositionsForShape(overrideShape, n);
        }
        else
        {
            if (n == 2)
            {
                if (centerAtom.Element.Symbol is "O" or "S")
                {
                    shape = "Bent";
                    angle = 104.5;
                    double rad = (180.0 - angle) * Math.PI / 360.0;
                    positions.Add(new Vector3D(-Math.Cos(rad) * 0.96, -Math.Sin(rad) * 0.96, 0));
                    positions.Add(new Vector3D(Math.Cos(rad) * 0.96, -Math.Sin(rad) * 0.96, 0));
                }
                else
                {
                    shape = "Linear";
                    angle = 180.0;
                    positions.Add(new Vector3D(-1.1, 0, 0));
                    positions.Add(new Vector3D(1.1, 0, 0));
                }
            }
            else if (n == 3)
            {
                if (centerAtom.Element.Symbol is "N" or "P")
                {
                    shape = "Trigonal Pyramidal";
                    angle = 107.0;
                    positions.Add(new Vector3D(0, 0.94, -0.35));
                    positions.Add(new Vector3D(-0.81, -0.47, -0.35));
                    positions.Add(new Vector3D(0.81, -0.47, -0.35));
                }
                else
                {
                    shape = "Trigonal Planar";
                    angle = 120.0;
                    for (int i = 0; i < 3; i++)
                    {
                        double aRad = i * 2.0 * Math.PI / 3.0;
                        positions.Add(new Vector3D(Math.Cos(aRad), Math.Sin(aRad), 0));
                    }
                }
            }
            else if (n == 4)
            {
                shape = "Tetrahedral";
                angle = 109.5;
                positions.Add(new Vector3D(0, 1.0, 0));
                positions.Add(new Vector3D(0.943, -0.333, 0));
                positions.Add(new Vector3D(-0.471, -0.333, 0.816));
                positions.Add(new Vector3D(-0.471, -0.333, -0.816));
            }
            else if (n == 5)
            {
                shape = "Trigonal Bipyramidal";
                angle = 90.0;
                positions.Add(new Vector3D(0, 1.1, 0));
                positions.Add(new Vector3D(0, -1.1, 0));
                positions.Add(new Vector3D(1.0, 0, 0));
                positions.Add(new Vector3D(-0.5, 0, 0.866));
                positions.Add(new Vector3D(-0.5, 0, -0.866));
            }
            else
            {
                shape = "Octahedral";
                angle = 90.0;
                positions.Add(new Vector3D(0, 1.1, 0));
                positions.Add(new Vector3D(0, -1.1, 0));
                positions.Add(new Vector3D(1.1, 0, 0));
                positions.Add(new Vector3D(-1.1, 0, 0));
                positions.Add(new Vector3D(0, 0, 1.1));
                positions.Add(new Vector3D(0, 0, -1.1));

                for (int i = 6; i < n; i++)
                {
                    positions.Add(new Vector3D((i % 2 == 0 ? 1 : -1) * 0.8, -0.8, (i % 3 == 0 ? 1 : -1) * 0.8));
                }
            }
        }

        var outerAtomIndices = molecule.Atoms.Select((atom, idx) => (atom, idx)).Where(x => x.idx != centerIndex).ToList();

        var atom3DArray = new Atom3D[molecule.Atoms.Count];
        atom3DArray[centerIndex] = new Atom3D(centerAtom, positions[0]);

        for (int i = 0; i < outerAtomIndices.Count; i++)
        {
            var pos = (i + 1 < positions.Count) ? positions[i + 1] : new Vector3D((i % 2 == 0 ? 1 : -1) * 0.8, -0.8, (i % 3 == 0 ? 1 : -1) * 0.8);
            int origIdx = outerAtomIndices[i].idx;
            atom3DArray[origIdx] = new Atom3D(outerAtomIndices[i].atom, pos);
        }

        return new Molecule3D(molecule.Name, molecule.ChemicalFormula, shape, angle, atom3DArray, molecule);
    }

    /// <summary>
    /// Computes spatial coordinates based on an explicit VSEPR geometric shape name.
    /// </summary>
    private static (double Angle, List<Vector3D> Positions) CalculatePositionsForShape(string shape, int outerAtomCount)
    {
        var pos = new List<Vector3D> { new(0, 0, 0) };
        double angle;

        switch (shape)
        {
            case "Linear":
                angle = 180.0;
                pos.Add(new Vector3D(-1.1, 0, 0));
                pos.Add(new Vector3D(1.1, 0, 0));
                break;

            case "Bent":
                angle = 104.5;
                double bentRad = (180.0 - angle) * Math.PI / 360.0;
                pos.Add(new Vector3D(-Math.Cos(bentRad) * 0.96, -Math.Sin(bentRad) * 0.96, 0));
                pos.Add(new Vector3D(Math.Cos(bentRad) * 0.96, -Math.Sin(bentRad) * 0.96, 0));
                break;

            case "Trigonal Planar":
                angle = 120.0;
                for (int i = 0; i < Math.Max(3, outerAtomCount); i++)
                {
                    double aRad = i * 2.0 * Math.PI / 3.0;
                    pos.Add(new Vector3D(Math.Cos(aRad), Math.Sin(aRad), 0));
                }
                break;

            case "Trigonal Pyramidal":
                angle = 107.0;
                pos.Add(new Vector3D(0, 0.94, -0.35));
                pos.Add(new Vector3D(-0.81, -0.47, -0.35));
                pos.Add(new Vector3D(0.81, -0.47, -0.35));
                break;

            case "Square Planar":
                angle = 90.0;
                pos.Add(new Vector3D(1.0, 0, 0));
                pos.Add(new Vector3D(-1.0, 0, 0));
                pos.Add(new Vector3D(0, 1.0, 0));
                pos.Add(new Vector3D(0, -1.0, 0));
                break;

            case "Trigonal Bipyramidal":
                angle = 90.0;
                pos.Add(new Vector3D(0, 1.1, 0));
                pos.Add(new Vector3D(0, -1.1, 0));
                pos.Add(new Vector3D(1.0, 0, 0));
                pos.Add(new Vector3D(-0.5, 0, 0.866));
                pos.Add(new Vector3D(-0.5, 0, -0.866));
                break;

            case "Octahedral":
                angle = 90.0;
                pos.Add(new Vector3D(0, 1.1, 0));
                pos.Add(new Vector3D(0, -1.1, 0));
                pos.Add(new Vector3D(1.1, 0, 0));
                pos.Add(new Vector3D(-1.1, 0, 0));
                pos.Add(new Vector3D(0, 0, 1.1));
                pos.Add(new Vector3D(0, 0, -1.1));
                break;

            case "Tetrahedral":
            default:
                angle = 109.5;
                pos.Add(new Vector3D(0, 1.0, 0));
                pos.Add(new Vector3D(0.943, -0.333, 0));
                pos.Add(new Vector3D(-0.471, -0.333, 0.816));
                pos.Add(new Vector3D(-0.471, -0.333, -0.816));
                break;
        }

        return (angle, pos);
    }
}
