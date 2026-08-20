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
            var diatomicList = new List<Atom3D>
            {
                new(molecule.Atoms[0], new Vector3D(-0.6, 0, 0)),
                new(molecule.Atoms[1], new Vector3D(0.6, 0, 0))
            };
            return new Molecule3D(molecule.Name, molecule.ChemicalFormula, "Linear", 180.0, diatomicList, molecule);
        }

        // Case 3: Single-center small polyatomic species vs Multi-center organic molecule
        int heavyAtomCount = molecule.Atoms.Count(a => a.Element.Symbol != "H");

        if (heavyAtomCount > 1 && string.IsNullOrWhiteSpace(overrideShape))
        {
            return GenerateMultiCenter3D(molecule);
        }

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

        var unoptimized = new Molecule3D(molecule.Name, molecule.ChemicalFormula, shape, angle, atom3DArray, molecule);
        return Physics.ForceFieldEngine.MinimizeEnergy(unoptimized, 80).MinimizedMolecule;
    }

    /// <summary>
    /// Embeds realistic, energy-minimized 3D coordinates for arbitrary branched, cyclic, or polycyclic organic molecules
    /// using ring-scaffold template embedding, valence-directed tetrahedral branching, and Universal Force Field (UFF) relaxation.
    /// </summary>
    private static Molecule3D GenerateMultiCenter3D(Molecule molecule)
    {
        int nAtoms = molecule.Atoms.Count;
        var coords = new Vector3D?[nAtoms];
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        var parentDirections = new Dictionary<int, Vector3D>();

        // 1. Detect cyclic scaffolds in the molecular graph
        var graph = Chemy.Core.Graph.ChemicalGraph.FromMolecule(molecule);
        var rings = graph.FindRings();

        if (rings.Count > 0)
        {
            // Embed the primary ring scaffold centered at (0, 0, 0) in the XY plane
            var primaryRing = rings.OrderByDescending(r => r.Count).First();
            int ringSize = primaryRing.Count;
            double bondLength = ringSize == 6 ? 1.395 : 1.40; // Aromatic C-C vs conjugated ring bond length
            double radius = bondLength / (2.0 * Math.Sin(Math.PI / ringSize));

            for (int k = 0; k < ringSize; k++)
            {
                int atomIdx = primaryRing[k];
                // Start along X axis and rotate counter-clockwise
                double theta = 2.0 * Math.PI * k / ringSize;
                var ringPos = new Vector3D(
                    Math.Round(radius * Math.Cos(theta), 4),
                    Math.Round(radius * Math.Sin(theta), 4),
                    0.0
                );
                coords[atomIdx] = ringPos;
                visited.Add(atomIdx);

                // Radial outward vector for substituent attachment
                var radialDir = Normalize(ringPos);
                parentDirections[atomIdx] = radialDir;
                queue.Enqueue(atomIdx);
            }
        }
        else
        {
            // Acyclic: Find root heavy atom with maximum connectivity
            int rootIndex = 0;
            int maxDegree = -1;
            for (int i = 0; i < nAtoms; i++)
            {
                if (molecule.Atoms[i].Element.Symbol != "H")
                {
                    int deg = molecule.Bonds.Count(b => b.Connects(i));
                    if (deg > maxDegree)
                    {
                        maxDegree = deg;
                        rootIndex = i;
                    }
                }
            }

            coords[rootIndex] = new Vector3D(0, 0, 0);
            visited.Add(rootIndex);
            parentDirections[rootIndex] = new Vector3D(1, 0, 0);
            queue.Enqueue(rootIndex);
        }

        // 2. Propagate substituents, aliphatic branches, and functional groups via valence geometry
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            var currentPos = coords[current]!;
            var refDir = parentDirections.TryGetValue(current, out var pd) ? pd : new Vector3D(1, 0, 0);

            var unplacedNeighbors = molecule.Bonds
                .Where(b => b.Connects(current))
                .Select(b => b.Atom1Index == current ? b.Atom2Index : b.Atom1Index)
                .Where(nbr => !visited.Contains(nbr))
                .ToList();

            var heavyNeighbors = unplacedNeighbors.Where(n => molecule.Atoms[n].Element.Symbol != "H").ToList();
            var hNeighbors = unplacedNeighbors.Where(n => molecule.Atoms[n].Element.Symbol == "H").ToList();

            var ortho1 = GetOrthogonalVector(refDir);
            var ortho2 = Normalize(Cross(refDir, ortho1));

            // Place heavy substituent neighbors with tetrahedral (109.5°) / trigonal (120°) geometry
            int numHeavy = heavyNeighbors.Count;
            for (int j = 0; j < numHeavy; j++)
            {
                int neighbor = heavyNeighbors[j];
                double bondLen = 1.51; // Standard C-C / C-O single bond length

                Vector3D outDir;
                if (numHeavy == 1)
                {
                    // Linear chain continuation: tetrahedral bend (109.5°) alternating torsion
                    double bendAngle = (180.0 - 109.5) * Math.PI / 180.0;
                    double torsion = (current % 2 == 0) ? 0.0 : Math.PI;
                    outDir = Normalize(new Vector3D(
                        Math.Cos(bendAngle) * refDir.X + Math.Sin(bendAngle) * (Math.Cos(torsion) * ortho1.X + Math.Sin(torsion) * ortho2.X),
                        Math.Cos(bendAngle) * refDir.Y + Math.Sin(bendAngle) * (Math.Cos(torsion) * ortho1.Y + Math.Sin(torsion) * ortho2.Y),
                        Math.Cos(bendAngle) * refDir.Z + Math.Sin(bendAngle) * (Math.Cos(torsion) * ortho1.Z + Math.Sin(torsion) * ortho2.Z)
                    ));
                }
                else if (numHeavy == 2)
                {
                    // Tetrahedral branching (e.g. isopropyl fork or propionic acid branch)
                    double bendAngle = (180.0 - 109.5) * Math.PI / 180.0;
                    double phi = (j == 0) ? (Math.PI / 3.0) : (-Math.PI / 3.0);
                    outDir = Normalize(new Vector3D(
                        Math.Cos(bendAngle) * refDir.X + Math.Sin(bendAngle) * (Math.Cos(phi) * ortho1.X + Math.Sin(phi) * ortho2.X),
                        Math.Cos(bendAngle) * refDir.Y + Math.Sin(bendAngle) * (Math.Cos(phi) * ortho1.Y + Math.Sin(phi) * ortho2.Y),
                        Math.Cos(bendAngle) * refDir.Z + Math.Sin(bendAngle) * (Math.Cos(phi) * ortho1.Z + Math.Sin(phi) * ortho2.Z)
                    ));
                }
                else
                {
                    // Quaternary tripod
                    double coneAngle = 70.5 * Math.PI / 180.0;
                    double rot = j * 2.0 * Math.PI / numHeavy;
                    outDir = Normalize(new Vector3D(
                        Math.Cos(coneAngle) * refDir.X + Math.Sin(coneAngle) * (Math.Cos(rot) * ortho1.X + Math.Sin(rot) * ortho2.X),
                        Math.Cos(coneAngle) * refDir.Y + Math.Sin(coneAngle) * (Math.Cos(rot) * ortho1.Y + Math.Sin(rot) * ortho2.Y),
                        Math.Cos(coneAngle) * refDir.Z + Math.Sin(coneAngle) * (Math.Cos(rot) * ortho1.Z + Math.Sin(rot) * ortho2.Z)
                    ));
                }

                coords[neighbor] = new Vector3D(
                    Math.Round(currentPos.X + outDir.X * bondLen, 4),
                    Math.Round(currentPos.Y + outDir.Y * bondLen, 4),
                    Math.Round(currentPos.Z + outDir.Z * bondLen, 4)
                );

                visited.Add(neighbor);
                parentDirections[neighbor] = outDir;
                queue.Enqueue(neighbor);
            }

            // Place hydrogen atoms symmetrically around the heavy atom
            int numH = hNeighbors.Count;
            for (int h = 0; h < numH; h++)
            {
                int hIdx = hNeighbors[h];
                double hDist = 1.09;
                Vector3D hDir;

                if (numHeavy == 0)
                {
                    // Terminal methyl / methane tripod
                    double coneAngle = 70.5 * Math.PI / 180.0;
                    double rot = h * 2.0 * Math.PI / Math.Max(1, numH);
                    hDir = Normalize(new Vector3D(
                        Math.Cos(coneAngle) * refDir.X + Math.Sin(coneAngle) * (Math.Cos(rot) * ortho1.X + Math.Sin(rot) * ortho2.X),
                        Math.Cos(coneAngle) * refDir.Y + Math.Sin(coneAngle) * (Math.Cos(rot) * ortho1.Y + Math.Sin(rot) * ortho2.Y),
                        Math.Cos(coneAngle) * refDir.Z + Math.Sin(coneAngle) * (Math.Cos(rot) * ortho1.Z + Math.Sin(rot) * ortho2.Z)
                    ));
                }
                else if (numHeavy == 1)
                {
                    // Methylene (-CH2-) or methine: straddle perpendicular to the bond plane
                    double bendAngle = (180.0 - 109.5) * Math.PI / 180.0;
                    double hRot = (h == 0) ? (Math.PI * 2.0 / 3.0) : (-Math.PI * 2.0 / 3.0);
                    hDir = Normalize(new Vector3D(
                        Math.Cos(bendAngle) * refDir.X + Math.Sin(bendAngle) * (Math.Cos(hRot) * ortho1.X + Math.Sin(hRot) * ortho2.X),
                        Math.Cos(bendAngle) * refDir.Y + Math.Sin(bendAngle) * (Math.Cos(hRot) * ortho1.Y + Math.Sin(hRot) * ortho2.Y),
                        Math.Cos(bendAngle) * refDir.Z + Math.Sin(bendAngle) * (Math.Cos(hRot) * ortho1.Z + Math.Sin(hRot) * ortho2.Z)
                    ));
                }
                else
                {
                    // Methine (-CH<) single hydrogen opposite to branches
                    hDir = Normalize(new Vector3D(
                        -refDir.X * 0.5 + ortho2.X * 0.866,
                        -refDir.Y * 0.5 + ortho2.Y * 0.866,
                        -refDir.Z * 0.5 + ortho2.Z * 0.866
                    ));
                }

                coords[hIdx] = new Vector3D(
                    Math.Round(currentPos.X + hDir.X * hDist, 4),
                    Math.Round(currentPos.Y + hDir.Y * hDist, 4),
                    Math.Round(currentPos.Z + hDir.Z * hDist, 4)
                );
                visited.Add(hIdx);
            }
        }

        // Place any remaining unplaced atoms
        for (int i = 0; i < nAtoms; i++)
        {
            if (coords[i] == null)
            {
                coords[i] = new Vector3D(i * 1.2, 0, 0);
            }
        }

        var atom3DList = new List<Atom3D>(nAtoms);
        for (int i = 0; i < nAtoms; i++)
        {
            atom3DList.Add(new Atom3D(molecule.Atoms[i], coords[i]!));
        }

        var unoptimized = new Molecule3D(molecule.Name, molecule.ChemicalFormula, "Conformer", 109.5, atom3DList, molecule);

        // Relax coordinates with Universal Force Field minimization (150 iterations)
        return Physics.ForceFieldEngine.MinimizeEnergy(unoptimized, 150).MinimizedMolecule;
    }

    private static Vector3D GetOrthogonalVector(Vector3D v)
    {
        var other = Math.Abs(v.X) < 0.8 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
        return Normalize(Cross(v, other));
    }

    private static Vector3D Cross(Vector3D a, Vector3D b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    private static Vector3D Normalize(Vector3D v)
    {
        double len = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return len < 1e-6 ? new Vector3D(0, 1, 0) : new Vector3D(v.X / len, v.Y / len, v.Z / len);
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

    /// <summary>
    /// Generates a planar 2D structural diagram representation of the molecule embedded in 3D space with Z = 0.0.
    /// Provides clear, uncluttered textbook ChemDraw-style layouts (regular polygon rings, 120° angles, zigzag chains)
    /// while retaining full 3D spatial rotation, lighting, and rendering in 3Dmol.js / WebGL.
    /// </summary>
    /// <param name="molecule">Input molecule.</param>
    /// <returns>Molecule3D with all atomic positions having Z = 0.0.</returns>
    public static Molecule3D GeneratePlanar3D(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        int atomCount = molecule.Atoms.Count;
        if (atomCount == 0)
        {
            return new Molecule3D(molecule.Name, molecule.ChemicalFormula, "Planar 2D", 120.0, Array.Empty<Atom3D>(), molecule);
        }

        if (atomCount == 1)
        {
            return new Molecule3D(molecule.Name, molecule.ChemicalFormula, "Planar 2D (Monatomic)", 0.0,
                new List<Atom3D> { new(molecule.Atoms[0], new Vector3D(0, 0, 0)) }, molecule);
        }

        if (atomCount == 2)
        {
            return new Molecule3D(molecule.Name, molecule.ChemicalFormula, "Planar 2D (Linear)", 180.0,
                new List<Atom3D>
                {
                    new(molecule.Atoms[0], new Vector3D(-0.7, 0, 0)),
                    new(molecule.Atoms[1], new Vector3D(0.7, 0, 0))
                }, molecule);
        }

        var coords = new Vector3D[atomCount];
        var placed = new bool[atomCount];

        // 1. Detect rings in the chemical graph
        var graph = Chemy.Core.Graph.ChemicalGraph.FromMolecule(molecule);
        var rings = graph.FindRings();

        // If there are rings, place the primary ring first centered at origin
        if (rings.Count > 0)
        {
            var primaryRing = rings.OrderByDescending(r => r.Count).First();
            int ringSize = primaryRing.Count;
            double radius = 1.40 / (2.0 * Math.Sin(Math.PI / ringSize)); // Regular polygon circumradius with edge ~1.40Å

            for (int k = 0; k < ringSize; k++)
            {
                int atomIdx = primaryRing[k];
                // Start from top and rotate clockwise in XY plane
                double theta = (Math.PI / 2.0) - (2.0 * Math.PI * k / ringSize);
                coords[atomIdx] = new Vector3D(
                    Math.Round(radius * Math.Cos(theta), 4),
                    Math.Round(radius * Math.Sin(theta), 4),
                    0.0
                );
                placed[atomIdx] = true;
            }
        }
        else
        {
            // No rings: Place first heavy atom at (0, 0, 0)
            int firstIdx = 0;
            for (int i = 0; i < atomCount; i++)
            {
                if (molecule.Atoms[i].Element.Symbol != "H") { firstIdx = i; break; }
            }
            coords[firstIdx] = new Vector3D(0, 0, 0);
            placed[firstIdx] = true;
        }

        // 2. Breadth-first layout of remaining heavy atoms in planar zigzag / radial patterns
        var queue = new Queue<int>();
        for (int i = 0; i < atomCount; i++)
        {
            if (placed[i]) queue.Enqueue(i);
        }

        // Keep track of parent directional angles
        var parentAngles = new Dictionary<int, double>();

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            var currentPos = coords[current];

            // Get unplaced neighbors
            var neighbors = molecule.Bonds
                .Where(b => b.Connects(current))
                .Select(b => b.Atom1Index == current ? b.Atom2Index : b.Atom1Index)
                .Where(n => !placed[n])
                .ToList();

            var heavyNeighbors = neighbors.Where(n => molecule.Atoms[n].Element.Symbol != "H").ToList();
            var hNeighbors = neighbors.Where(n => molecule.Atoms[n].Element.Symbol == "H").ToList();

            double baseAngle = parentAngles.TryGetValue(current, out var pa) ? pa + Math.PI : 0.0;
            if (placed.Count(p => p) <= 6 && rings.Count > 0 && (currentPos.X != 0 || currentPos.Y != 0))
            {
                // Radiate outward from ring center
                baseAngle = Math.Atan2(currentPos.Y, currentPos.X);
            }

            // Place heavy neighbors at 120° / 60° / zigzag offsets
            for (int j = 0; j < heavyNeighbors.Count; j++)
            {
                int neighbor = heavyNeighbors[j];
                double angleOffset;
                if (heavyNeighbors.Count == 1)
                {
                    // Zigzag alternating +/- 30 degrees
                    angleOffset = (current % 2 == 0) ? (Math.PI / 6.0) : (-Math.PI / 6.0);
                }
                else
                {
                    double span = Math.PI * 0.8;
                    angleOffset = -span / 2.0 + (span * (j + 0.5) / heavyNeighbors.Count);
                }

                double angle = baseAngle + angleOffset;
                double bondLen = 1.45;

                coords[neighbor] = new Vector3D(
                    Math.Round(currentPos.X + bondLen * Math.Cos(angle), 4),
                    Math.Round(currentPos.Y + bondLen * Math.Sin(angle), 4),
                    0.0
                );
                placed[neighbor] = true;
                parentAngles[neighbor] = angle;
                queue.Enqueue(neighbor);
            }

            // Place hydrogens around parent atom symmetrically on the XY plane
            for (int h = 0; h < hNeighbors.Count; h++)
            {
                int hIdx = hNeighbors[h];
                double hAngle;
                if (heavyNeighbors.Count == 0)
                {
                    hAngle = baseAngle + (2.0 * Math.PI * h / Math.Max(1, hNeighbors.Count));
                }
                else
                {
                    double hSpan = Math.PI * 0.7;
                    hAngle = baseAngle + Math.PI - (hSpan / 2.0) + (hSpan * (h + 0.5) / hNeighbors.Count);
                }

                double hDist = 1.00;
                coords[hIdx] = new Vector3D(
                    Math.Round(currentPos.X + hDist * Math.Cos(hAngle), 4),
                    Math.Round(currentPos.Y + hDist * Math.Sin(hAngle), 4),
                    0.0
                );
                placed[hIdx] = true;
            }
        }

        // Place any remaining unplaced disconnected atoms
        for (int i = 0; i < atomCount; i++)
        {
            if (!placed[i])
            {
                coords[i] = new Vector3D(i * 1.5, 0, 0);
                placed[i] = true;
            }
        }

        // 3. Center planar coordinates at centroid (0, 0, 0)
        double cx = coords.Average(p => p.X);
        double cy = coords.Average(p => p.Y);

        var atom3dList = new List<Atom3D>(atomCount);
        for (int i = 0; i < atomCount; i++)
        {
            atom3dList.Add(new Atom3D(
                molecule.Atoms[i],
                new Vector3D(
                    Math.Round(coords[i].X - cx, 4),
                    Math.Round(coords[i].Y - cy, 4),
                    0.0
                )
            ));
        }

        return new Molecule3D(
            molecule.Name,
            molecule.ChemicalFormula,
            "Planar 2D (ChemDraw Style)",
            120.0,
            atom3dList,
            molecule
        );
    }
}
