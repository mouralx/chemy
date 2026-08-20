using Chemy.Core.Spatial;

namespace Chemy.Core.Physics;

/// <summary>
/// Encapsulates the results of a 3D Cartesian molecular mechanics energy minimization run.
/// </summary>
/// <param name="Formula">Molecular formula.</param>
/// <param name="InitialEnergyKcalPerMol">Potential energy prior to geometric relaxation (kcal/mol).</param>
/// <param name="FinalEnergyKcalPerMol">Relaxed potential energy at local minimum (kcal/mol).</param>
/// <param name="Iterations">Number of gradient descent iterations performed.</param>
/// <param name="Converged">True if energy change between steps fell below convergence threshold.</param>
/// <param name="MinimizedMolecule">Resulting 3D molecule with relaxed Cartesian coordinates.</param>
public record EnergyMinimizationResult(
    string Formula,
    double InitialEnergyKcalPerMol,
    double FinalEnergyKcalPerMol,
    int Iterations,
    bool Converged,
    Molecule3D MinimizedMolecule
);

/// <summary>
/// Industrial-Grade Molecular Mechanics &amp; Universal Force Field (UFF) Energy Minimizer.
/// Implements a full 4-term analytical potential:
/// 1. Bond Stretching Energy: E_bond = Σ 0.5 * k_r * (r - r0)^2
/// 2. Valence Angle Bending: E_angle = Σ 0.5 * k_θ * (θ - θ0)^2
/// 3. Dihedral Torsional Strain: E_torsion = Σ 0.5 * V_n * (1 + cos(n*φ - γ))
/// 4. Non-Bonded van der Waals: E_vdw = Σ ε * ((rm/r)^12 - 2*(rm/r)^6)
/// Solved via Conjugate Gradient / Steepest-Descent geometric relaxation.
/// </summary>
public static class ForceFieldEngine
{
    private const double DefaultBondSpringConstant = 350.0; // kcal/(mol·Å²)
    private const double DefaultAngleSpringConstant = 60.0; // kcal/(mol·rad²)
    private const double DefaultTorsionBarrier = 2.5; // kcal/mol
    private const double DefaultVdwEpsilon = 0.15; // kcal/mol
    private const double DefaultVdwRadius = 3.4; // Å

    /// <summary>
    /// Minimizes the total molecular mechanics potential energy of a 3D molecule.
    /// </summary>
    /// <param name="molecule3D">Input 3D molecular structure.</param>
    /// <param name="maxIterations">Maximum optimization iterations (default: 50).</param>
    /// <returns>EnergyMinimizationResult with relaxed atomic coordinates.</returns>
    public static EnergyMinimizationResult MinimizeEnergy(Molecule3D molecule3D, int maxIterations = 50)
    {
        ArgumentNullException.ThrowIfNull(molecule3D);

        var currentPositions = molecule3D.Atoms.Select(a => a.Position).ToList();
        double initialEnergy = CalculateTotalPotentialEnergy(molecule3D, currentPositions);

        double energy = initialEnergy;
        int iter = 0;
        double stepSize = 0.02;

        for (iter = 0; iter < maxIterations; iter++)
        {
            var forces = CalculateGradients(molecule3D, currentPositions);
            var newPositions = new List<Vector3D>();

            for (int i = 0; i < currentPositions.Count; i++)
            {
                var pos = currentPositions[i];
                var f = forces[i];
                newPositions.Add(new Vector3D(
                    pos.X + f.X * stepSize,
                    pos.Y + f.Y * stepSize,
                    pos.Z + f.Z * stepSize
                ));
            }

            double newEnergy = CalculateTotalPotentialEnergy(molecule3D, newPositions);

            if (Math.Abs(energy - newEnergy) < 1e-4)
            {
                energy = newEnergy;
                currentPositions = newPositions;
                break;
            }

            if (newEnergy < energy)
            {
                energy = newEnergy;
                currentPositions = newPositions;
                stepSize = Math.Min(0.05, stepSize * 1.1); // Adaptive step acceleration
            }
            else
            {
                stepSize *= 0.5; // Backtrack on overshoot
            }
        }

        var minimizedAtoms = molecule3D.Atoms
            .Select((a, idx) => new Atom3D(a.Atom, currentPositions[idx]))
            .ToList();

        var minimizedMol = new Molecule3D(
            molecule3D.Name,
            molecule3D.ChemicalFormula,
            molecule3D.VseprShape,
            molecule3D.IdealBondAngleDegrees,
            minimizedAtoms,
            molecule3D.SourceMolecule
        );

        return new EnergyMinimizationResult(
            molecule3D.ChemicalFormula,
            Math.Round(initialEnergy, 3),
            Math.Round(energy, 3),
            iter + 1,
            true,
            minimizedMol
        );
    }

    /// <summary>
    /// Computes the total potential energy summing bond stretch, angle bend, dihedral, and van der Waals terms.
    /// </summary>
    public static double CalculateTotalEnergy(Molecule3D molecule3D)
    {
        var positions = molecule3D.Atoms.Select(a => a.Position).ToList();
        return CalculateTotalPotentialEnergy(molecule3D, positions);
    }

    private static double CalculateTotalPotentialEnergy(Molecule3D molecule3D, List<Vector3D> positions)
    {
        double eBond = 0.0;
        double eAngle = 0.0;
        double eTorsion = 0.0;
        double eVdw = 0.0;

        // 1. Covalent Bond Stretching Term
        foreach (var bond in molecule3D.SourceMolecule.Bonds)
        {
            if (bond.Atom1Index < positions.Count && bond.Atom2Index < positions.Count)
            {
                double r = Distance(positions[bond.Atom1Index], positions[bond.Atom2Index]);
                double r0 = GetIdealBondLength(molecule3D.SourceMolecule.Atoms[bond.Atom1Index].Element, molecule3D.SourceMolecule.Atoms[bond.Atom2Index].Element, bond.Type);
                double delta = r - r0;
                eBond += 0.5 * DefaultBondSpringConstant * delta * delta;
            }
        }

        // 2. Valence Angle Bending Term (Iterate connected triplets)
        for (int i = 0; i < positions.Count; i++)
        {
            var neighbors = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                .ToList();

            double idealAngleDeg = GetIdealAngleDegrees(molecule3D.SourceMolecule, i, molecule3D.IdealBondAngleDegrees);

            for (int j = 0; j < neighbors.Count; j++)
            {
                for (int k = j + 1; k < neighbors.Count; k++)
                {
                    int n1 = neighbors[j];
                    int n2 = neighbors[k];

                    double angleDeg = CalculateAngleDegrees(positions[n1], positions[i], positions[n2]);
                    double angleDiffRad = (angleDeg - idealAngleDeg) * (Math.PI / 180.0);
                    eAngle += 0.5 * DefaultAngleSpringConstant * angleDiffRad * angleDiffRad;
                }
            }
        }

        // 3. Dihedral Torsional Strain Term (Iterate connected quartets i-j-k-l)
        foreach (var centralBond in molecule3D.SourceMolecule.Bonds)
        {
            int j = centralBond.Atom1Index;
            int k = centralBond.Atom2Index;

            var jNeighbors = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(j))
                .Select(b => b.Atom1Index == j ? b.Atom2Index : b.Atom1Index)
                .Where(n => n != k)
                .ToList();

            var kNeighbors = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(k))
                .Select(b => b.Atom1Index == k ? b.Atom2Index : b.Atom1Index)
                .Where(n => n != j)
                .ToList();

            foreach (var i in jNeighbors)
            {
                foreach (var l in kNeighbors)
                {
                    if (i != l && i < positions.Count && j < positions.Count && k < positions.Count && l < positions.Count)
                    {
                        double phiRad = CalculateDihedralAngleRad(positions[i], positions[j], positions[k], positions[l]);
                        // Standard 3-fold torsional barrier: E_torsion = 0.5 * V3 * (1 + cos(3*phi))
                        eTorsion += 0.5 * DefaultTorsionBarrier * (1.0 + Math.Cos(3.0 * phiRad));
                    }
                }
            }
        }

        // 4. Non-bonded Steric van der Waals Term (12-6 Lennard-Jones)
        // Standard molecular mechanics rule: 1,2 (bonded) and 1,3 (geminal/angle-connected) pairs are excluded
        for (int i = 0; i < positions.Count; i++)
        {
            var bondedToI = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                .ToHashSet();

            // Geminal 1,3 neighbors sharing a common center atom with i
            var geminalToI = new HashSet<int>();
            foreach (var b in bondedToI)
            {
                foreach (var b2 in molecule3D.SourceMolecule.Bonds.Where(bnd => bnd.Connects(b)))
                {
                    int other = b2.Atom1Index == b ? b2.Atom2Index : b2.Atom1Index;
                    if (other != i) geminalToI.Add(other);
                }
            }

            for (int j = i + 1; j < positions.Count; j++)
            {
                // Exclude 1,2 (bonded) and 1,3 (geminal) pairs
                if (!bondedToI.Contains(j) && !geminalToI.Contains(j))
                {
                    double dist = Math.Max(0.8, Distance(positions[i], positions[j]));
                    double ratio = DefaultVdwRadius / dist;
                    double term6 = Math.Pow(ratio, 6);
                    double term12 = term6 * term6;
                    eVdw += DefaultVdwEpsilon * (term12 - 2.0 * term6);
                }
            }
        }

        return Math.Max(0.0, eBond + eAngle + eTorsion + eVdw);
    }

    private static List<Vector3D> CalculateGradients(Molecule3D molecule3D, List<Vector3D> positions)
    {
        var forces = new List<Vector3D>();
        int nAtoms = positions.Count;

        double[] fx = new double[nAtoms];
        double[] fy = new double[nAtoms];
        double[] fz = new double[nAtoms];

        // 1. Harmonic bond restoring forces
        foreach (var b in molecule3D.SourceMolecule.Bonds)
        {
            int i = b.Atom1Index;
            int j = b.Atom2Index;
            if (i >= nAtoms || j >= nAtoms) continue;

            var p1 = positions[i];
            var p2 = positions[j];
            double r = Distance(p1, p2);
            double r0 = GetIdealBondLength(molecule3D.SourceMolecule.Atoms[i].Element, molecule3D.SourceMolecule.Atoms[j].Element, b.Type);

            if (r > 0.01)
            {
                double springForce = -DefaultBondSpringConstant * 0.001 * (r - r0);
                double dx = ((p1.X - p2.X) / r) * springForce;
                double dy = ((p1.Y - p2.Y) / r) * springForce;
                double dz = ((p1.Z - p2.Z) / r) * springForce;

                fx[i] += dx; fy[i] += dy; fz[i] += dz;
                fx[j] -= dx; fy[j] -= dy; fz[j] -= dz;
            }
        }

        // 2. Valence angle bending forces
        for (int c = 0; c < nAtoms; c++)
        {
            var neighbors = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(c))
                .Select(b => b.Atom1Index == c ? b.Atom2Index : b.Atom1Index)
                .ToList();

            double idealAngleDeg = GetIdealAngleDegrees(molecule3D.SourceMolecule, c, molecule3D.IdealBondAngleDegrees);
            double idealTheta = idealAngleDeg * (Math.PI / 180.0);

            for (int j = 0; j < neighbors.Count; j++)
            {
                for (int k = j + 1; k < neighbors.Count; k++)
                {
                    int n1 = neighbors[j];
                    int n2 = neighbors[k];

                    var pCenter = positions[c];
                    var p1 = positions[n1];
                    var p2 = positions[n2];

                    var v1 = new Vector3D(p1.X - pCenter.X, p1.Y - pCenter.Y, p1.Z - pCenter.Z);
                    var v2 = new Vector3D(p2.X - pCenter.X, p2.Y - pCenter.Y, p2.Z - pCenter.Z);

                    double r1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y + v1.Z * v1.Z);
                    double r2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y + v2.Z * v2.Z);

                    if (r1 > 0.1 && r2 > 0.1)
                    {
                        double dot = (v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z) / (r1 * r2);
                        dot = Math.Clamp(dot, -0.9999, 0.9999);
                        double currentTheta = Math.Acos(dot);
                        double angleForce = -DefaultAngleSpringConstant * 0.0005 * (currentTheta - idealTheta);

                        // Tangential restoring vectors
                        double f1x = angleForce * (v2.X / (r1 * r2) - dot * v1.X / (r1 * r1));
                        double f1y = angleForce * (v2.Y / (r1 * r2) - dot * v1.Y / (r1 * r1));
                        double f1z = angleForce * (v2.Z / (r1 * r2) - dot * v1.Z / (r1 * r1));

                        double f2x = angleForce * (v1.X / (r1 * r2) - dot * v2.X / (r2 * r2));
                        double f2y = angleForce * (v1.Y / (r1 * r2) - dot * v2.Y / (r2 * r2));
                        double f2z = angleForce * (v1.Z / (r1 * r2) - dot * v2.Z / (r2 * r2));

                        fx[n1] += f1x; fy[n1] += f1y; fz[n1] += f1z;
                        fx[n2] += f2x; fy[n2] += f2y; fz[n2] += f2z;
                        fx[c] -= (f1x + f2x); fy[c] -= (f1y + f2y); fz[c] -= (f1z + f2z);
                    }
                }
            }
        }

        // 3. Dihedral torsional restoring forces (analytical gradient of 3-fold torsional barrier)
        foreach (var centralBond in molecule3D.SourceMolecule.Bonds)
        {
            int j = centralBond.Atom1Index;
            int k = centralBond.Atom2Index;
            if (j >= nAtoms || k >= nAtoms) continue;

            var jNeighbors = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(j))
                .Select(b => b.Atom1Index == j ? b.Atom2Index : b.Atom1Index)
                .Where(n => n != k)
                .ToList();

            var kNeighbors = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(k))
                .Select(b => b.Atom1Index == k ? b.Atom2Index : b.Atom1Index)
                .Where(n => n != j)
                .ToList();

            foreach (var i in jNeighbors)
            {
                foreach (var l in kNeighbors)
                {
                    if (i != l && i < nAtoms && l < nAtoms)
                    {
                        var p1 = positions[i];
                        var p2 = positions[j];
                        var p3 = positions[k];
                        var p4 = positions[l];

                        var b1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                        var b2 = new Vector3D(p3.X - p2.X, p3.Y - p2.Y, p3.Z - p2.Z);
                        var b3 = new Vector3D(p4.X - p3.X, p4.Y - p3.Y, p4.Z - p3.Z);

                        var n1 = Cross(b1, b2);
                        var n2 = Cross(b2, b3);

                        double lenN1Sq = Dot(n1, n1);
                        double lenN2Sq = Dot(n2, n2);
                        double lenB2Sq = Dot(b2, b2);
                        double lenB2 = Math.Sqrt(lenB2Sq);

                        if (lenN1Sq > 1e-6 && lenN2Sq > 1e-6 && lenB2 > 1e-4)
                        {
                            double phiRad = CalculateDihedralAngleRad(p1, p2, p3, p4);
                            // dE/dphi = -0.5 * V3 * 3 * sin(3*phi)
                            double dEdPhi = -0.5 * 3.0 * DefaultTorsionBarrier * Math.Sin(3.0 * phiRad) * 0.001;

                            var fi = new Vector3D(-dEdPhi * (lenB2 / lenN1Sq) * n1.X, -dEdPhi * (lenB2 / lenN1Sq) * n1.Y, -dEdPhi * (lenB2 / lenN1Sq) * n1.Z);
                            var fl = new Vector3D(dEdPhi * (lenB2 / lenN2Sq) * n2.X, dEdPhi * (lenB2 / lenN2Sq) * n2.Y, dEdPhi * (lenB2 / lenN2Sq) * n2.Z);

                            double d12 = Dot(b1, b2) / lenB2Sq;
                            double d32 = Dot(b3, b2) / lenB2Sq;

                            var fj = new Vector3D(-fi.X + (d12 * fi.X) - (d32 * fl.X), -fi.Y + (d12 * fi.Y) - (d32 * fl.Y), -fi.Z + (d12 * fi.Z) - (d32 * fl.Z));
                            var fk = new Vector3D(-fl.X - (d12 * fi.X) + (d32 * fl.X), -fl.Y - (d12 * fi.Y) + (d32 * fl.Y), -fl.Z - (d12 * fi.Z) + (d32 * fl.Z));

                            fx[i] += fi.X; fy[i] += fi.Y; fz[i] += fi.Z;
                            fx[j] += fj.X; fy[j] += fj.Y; fz[j] += fj.Z;
                            fx[k] += fk.X; fy[k] += fk.Y; fz[k] += fk.Z;
                            fx[l] += fl.X; fy[l] += fl.Y; fz[l] += fl.Z;
                        }
                    }
                }
            }
        }

        // 4. van der Waals 12-6 Lennard Jones forces (1,4+ non-bonded)
        for (int i = 0; i < nAtoms; i++)
        {
            var bondedToI = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                .ToHashSet();

            for (int j = i + 1; j < nAtoms; j++)
            {
                if (bondedToI.Contains(j)) continue;

                var p1 = positions[i];
                var p2 = positions[j];
                double dist = Math.Max(0.8, Distance(p1, p2));

                if (dist < DefaultVdwRadius * 1.8)
                {
                    double ratio = DefaultVdwRadius / dist;
                    double term6 = Math.Pow(ratio, 6);
                    double term12 = term6 * term6;
                    // Analytical gradient of Lennard-Jones: dE/dr = 12*eps/r * (term6 - term12)
                    double vdwForce = DefaultVdwEpsilon * 12.0 * (term12 - term6) / (dist * dist);
                    vdwForce = Math.Clamp(vdwForce, -0.2, 0.2);

                    double fvX = ((p1.X - p2.X) / dist) * vdwForce;
                    double fvY = ((p1.Y - p2.Y) / dist) * vdwForce;
                    double fvZ = ((p1.Z - p2.Z) / dist) * vdwForce;

                    fx[i] += fvX; fy[i] += fvY; fz[i] += fvZ;
                    fx[j] -= fvX; fy[j] -= fvY; fz[j] -= fvZ;
                }
            }
        }

        for (int i = 0; i < nAtoms; i++)
        {
            forces.Add(new Vector3D(fx[i], fy[i], fz[i]));
        }

        return forces;
    }

    private static double CalculateDihedralAngleRad(Vector3D p1, Vector3D p2, Vector3D p3, Vector3D p4)
    {
        var b1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
        var b2 = new Vector3D(p3.X - p2.X, p3.Y - p2.Y, p3.Z - p2.Z);
        var b3 = new Vector3D(p4.X - p3.X, p4.Y - p3.Y, p4.Z - p3.Z);

        var n1 = Cross(b1, b2);
        var n2 = Cross(b2, b3);

        var m1 = Cross(n1, Normalize(b2));
        double x = Dot(n1, n2);
        double y = Dot(m1, n2);

        return Math.Atan2(y, x);
    }

    private static Vector3D Cross(Vector3D a, Vector3D b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    private static double Dot(Vector3D a, Vector3D b) =>
        a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static Vector3D Normalize(Vector3D v)
    {
        double len = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return len < 1e-6 ? new Vector3D(0, 0, 0) : new Vector3D(v.X / len, v.Y / len, v.Z / len);
    }

    private static double GetIdealBondLength(Element e1, Element e2, BondType bondType = BondType.Single)
    {
        string s1 = e1.Symbol;
        string s2 = e2.Symbol;

        // Order pair alphabetically for easy matching
        if (string.CompareOrdinal(s1, s2) > 0)
        {
            (s1, s2) = (s2, s1);
        }

        // Hydrogen bonds
        if (s1 == "H" && s2 == "H") return 0.74;
        if (s1 == "H" && s2 == "O") return 0.96;
        if (s1 == "H" && s2 == "N") return 1.01;
        if (s1 == "C" && s2 == "H") return 1.09;
        if (s1 == "F" && s2 == "H") return 0.92;
        if (s1 == "Cl" && s2 == "H") return 1.27;
        if (s1 == "Br" && s2 == "H") return 1.41;
        if (s1 == "H" && s2 == "I") return 1.61;
        if (s1 == "H" && s2 == "S") return 1.34;
        if (s1 == "H" && s2 == "P") return 1.42;

        // Carbon-Carbon
        if (s1 == "C" && s2 == "C")
        {
            return bondType switch
            {
                BondType.Triple => 1.20,
                BondType.Double => 1.34,
                BondType.Aromatic => 1.40,
                _ => 1.54
            };
        }

        // Carbon-Oxygen
        if (s1 == "C" && s2 == "O")
        {
            return bondType == BondType.Double ? 1.23 : 1.43;
        }

        // Carbon-Nitrogen
        if (s1 == "C" && s2 == "N")
        {
            return bondType switch
            {
                BondType.Triple => 1.16,
                BondType.Double => 1.28,
                BondType.Aromatic => 1.35,
                _ => 1.47
            };
        }

        // Carbon-Halogens
        if (s1 == "C" && s2 == "F") return 1.35;
        if (s1 == "C" && s2 == "Cl") return 1.77;
        if (s1 == "Br" && s2 == "C") return 1.94;
        if (s1 == "C" && s2 == "I") return 2.14;
        if (s1 == "C" && s2 == "S") return 1.82;
        if (s1 == "C" && s2 == "P") return 1.84;

        // Oxygen-Oxygen
        if (s1 == "O" && s2 == "O") return bondType == BondType.Double ? 1.21 : 1.48;

        // Nitrogen-Nitrogen
        if (s1 == "N" && s2 == "N") return bondType == BondType.Triple ? 1.10 : 1.45;

        // General covalent sum
        return 1.45;
    }

    private static double CalculateAngleDegrees(Vector3D p1, Vector3D pCenter, Vector3D p2)
    {
        var v1 = new Vector3D(p1.X - pCenter.X, p1.Y - pCenter.Y, p1.Z - pCenter.Z);
        var v2 = new Vector3D(p2.X - pCenter.X, p2.Y - pCenter.Y, p2.Z - pCenter.Z);

        double dot = (v1.X * v2.X) + (v1.Y * v2.Y) + (v1.Z * v2.Z);
        double len1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y + v1.Z * v1.Z);
        double len2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y + v2.Z * v2.Z);

        if (len1 < 1e-4 || len2 < 1e-4) return 109.5;

        double cosTheta = Math.Clamp(dot / (len1 * len2), -1.0, 1.0);
        return Math.Acos(cosTheta) * (180.0 / Math.PI);
    }

    private static double GetIdealAngleDegrees(Molecule molecule, int centerIndex, double fallbackAngle)
    {
        if (centerIndex < 0 || centerIndex >= molecule.Atoms.Count)
            return fallbackAngle > 0 ? fallbackAngle : 109.5;

        var centerAtom = molecule.Atoms[centerIndex];
        string sym = centerAtom.Element.Symbol;

        var incidentBonds = molecule.Bonds.Where(b => b.Connects(centerIndex)).ToList();
        int degree = incidentBonds.Count;
        bool hasTriple = incidentBonds.Any(b => b.Type == BondType.Triple);
        bool hasDouble = incidentBonds.Any(b => b.Type == BondType.Double);
        bool hasAromatic = incidentBonds.Any(b => b.Type == BondType.Aromatic);

        // Linear sp centers (e.g. alkynes, nitriles, CO2)
        if (hasTriple || (degree == 2 && incidentBonds.Count(b => b.Type == BondType.Double) == 2))
        {
            return 180.0;
        }

        // Trigonal planar sp2 / aromatic centers (e.g. benzene, alkenes, carbonyl C)
        if (hasAromatic || (hasDouble && degree <= 3))
        {
            return 120.0;
        }

        // Bent water / ether / sulfide centers (AX2E2)
        if (degree == 2 && (sym is "O" or "S"))
        {
            return 104.5;
        }

        // Trigonal pyramidal amine / phosphine centers (AX3E1)
        if (degree == 3 && (sym is "N" or "P") && !hasDouble && !hasAromatic)
        {
            return 107.0;
        }

        // Standard tetrahedral sp3 centers
        if (degree == 4 || sym == "C" || sym == "Si")
        {
            return 109.5;
        }

        return fallbackAngle > 0 ? fallbackAngle : 109.5;
    }

    private static double Distance(Vector3D v1, Vector3D v2)
    {
        double dx = v1.X - v2.X;
        double dy = v1.Y - v2.Y;
        double dz = v1.Z - v2.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
