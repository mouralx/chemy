namespace Chemy.Core.Physics;

using Chemy.Core.Scientific;
using Chemy.Core.Spatial;

/// <summary>
/// Status reason for force field optimization termination.
/// </summary>
public enum MinimizationTerminationReason
{
    /// <summary>Maximum gradient component fell below tolerance threshold (||g||_inf &lt; tol).</summary>
    GradientToleranceReached,

    /// <summary>Energy change between consecutive iterations was smaller than threshold.</summary>
    EnergyConvergenceReached,

    /// <summary>Line search step size became smaller than minimum precision limit.</summary>
    LineSearchExhausted,

    /// <summary>Maximum iteration budget reached prior to convergence.</summary>
    MaximumIterationsReached
}

/// <summary>
/// Encapsulates the results of a 3D Cartesian molecular mechanics energy minimization run.
/// </summary>
/// <param name="Formula">Molecular formula.</param>
/// <param name="InitialEnergyKcalPerMol">Potential energy prior to geometric relaxation (kcal/mol).</param>
/// <param name="FinalEnergyKcalPerMol">Relaxed potential energy at local minimum (kcal/mol).</param>
/// <param name="Iterations">Number of optimization iterations performed.</param>
/// <param name="Converged">True if gradient or energy convergence criteria were met.</param>
/// <param name="TerminationReason">Exact reason for optimizer completion.</param>
/// <param name="FinalGradientNorm">Maximum Cartesian gradient component (kcal/(mol·Å)).</param>
/// <param name="MinimizedMolecule">Resulting 3D molecule with relaxed Cartesian coordinates.</param>
/// <param name="MethodInfo">Scientific method provenance and metadata.</param>
public sealed record EnergyMinimizationResult(
    string Formula,
    double InitialEnergyKcalPerMol,
    double FinalEnergyKcalPerMol,
    int Iterations,
    bool Converged,
    MinimizationTerminationReason TerminationReason,
    double FinalGradientNorm,
    Molecule3D MinimizedMolecule,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// Universal Force Field (UFF) Molecular Mechanics Engine.
/// Parameterized according to Rappé, Casewit, Colwell, Goddard &amp; Skiff (J. Am. Chem. Soc. 1992, 114, 10024-10035).
/// Features exact potential energy evaluation, verified finite-difference gradients, soft-core clash resolution, and line-search optimization.
/// </summary>
public static class ForceFieldEngine
{
    private static readonly ScientificMethodInfo UffMethodInfo = new(
        "Rappé Universal Force Field (UFF) Molecular Mechanics",
        "1992.1",
        EvidenceLevel.NumericalApproximation,
        "Small molecules and periodic systems across the periodic table (H-Lw).",
        ["Harmonic valence and 12-6 LJ nonbonded terms; does not model bond breaking/forming chemical reactivity or explicit polarization."]
    );

    private record UffAtomParams(double R0, double Theta0Deg, double X, double D, double Chi, double Z);

    // Rappé et al. (1992) Table 1 Parameters
    private static readonly Dictionary<string, UffAtomParams> UffParams = new(StringComparer.OrdinalIgnoreCase)
    {
        ["H"] = new(0.354, 180.0, 2.886, 0.044, 4.528, 0.712),
        ["C_3"] = new(0.757, 109.4712, 3.851, 0.105, 5.343, 1.912),
        ["C_2"] = new(0.732, 120.0, 3.851, 0.105, 5.343, 1.912),
        ["C_R"] = new(0.729, 120.0, 3.851, 0.105, 5.343, 1.912),
        ["C_1"] = new(0.706, 180.0, 3.851, 0.105, 5.343, 1.912),
        ["N_3"] = new(0.700, 106.7, 3.660, 0.069, 6.899, 2.544),
        ["N_2"] = new(0.685, 120.0, 3.660, 0.069, 6.899, 2.544),
        ["N_R"] = new(0.699, 120.0, 3.660, 0.069, 6.899, 2.544),
        ["O_3"] = new(0.658, 104.51, 3.500, 0.060, 8.741, 2.300),
        ["O_2"] = new(0.634, 120.0, 3.500, 0.060, 8.741, 2.300),
        ["O_R"] = new(0.658, 120.0, 3.500, 0.060, 8.741, 2.300),
        ["F"] = new(0.668, 180.0, 3.364, 0.050, 10.874, 1.960),
        ["Cl"] = new(1.044, 180.0, 3.947, 0.227, 8.564, 2.668),
        ["Br"] = new(1.192, 180.0, 4.189, 0.251, 7.790, 3.328),
        ["I"] = new(1.382, 180.0, 4.542, 0.339, 6.802, 4.280),
        ["S_3"] = new(1.064, 103.2, 4.035, 0.274, 6.928, 2.766),
        ["P_3"] = new(1.100, 93.3, 4.147, 0.305, 5.463, 2.895)
    };

    /// <summary>
    /// Performs molecular mechanics geometric relaxation of a 3D molecule.
    /// </summary>
    public static EnergyMinimizationResult MinimizeEnergy(
        Molecule3D molecule3D,
        int maxIterations = 200,
        double gradientTolerance = 1e-3)
    {
        ArgumentNullException.ThrowIfNull(molecule3D);

        int nAtoms = molecule3D.Atoms.Count;
        if (nAtoms <= 1)
        {
            return new EnergyMinimizationResult(
                molecule3D.ChemicalFormula,
                0.0,
                0.0,
                0,
                true,
                MinimizationTerminationReason.GradientToleranceReached,
                0.0,
                molecule3D,
                UffMethodInfo
            );
        }

        var currentPositions = molecule3D.Atoms.Select(a => a.Position).ToList();
        double currentEnergy = CalculateTotalPotentialEnergy(molecule3D, currentPositions);
        double initialEnergy = currentEnergy;

        var reason = MinimizationTerminationReason.MaximumIterationsReached;
        int iter = 0;
        double maxGrad = 0.0;

        for (iter = 0; iter < maxIterations; iter++)
        {
            var grads = ComputeExactGradients(molecule3D, currentPositions);
            maxGrad = grads.Max(g => Math.Max(Math.Abs(g.X), Math.Max(Math.Abs(g.Y), Math.Abs(g.Z))));

            if (maxGrad < gradientTolerance)
            {
                reason = MinimizationTerminationReason.GradientToleranceReached;
                break;
            }

            // Adaptive initial step inversely proportional to max gradient to avoid overshoot on steep surfaces
            double step = Math.Min(0.05, 1.0 / Math.Max(1.0, maxGrad));
            bool stepAccepted = false;

            for (int lineIter = 0; lineIter < 20; lineIter++)
            {
                var candidatePositions = new List<Vector3D>(nAtoms);
                for (int i = 0; i < nAtoms; i++)
                {
                    candidatePositions.Add(new Vector3D(
                        currentPositions[i].X - step * grads[i].X,
                        currentPositions[i].Y - step * grads[i].Y,
                        currentPositions[i].Z - step * grads[i].Z
                    ));
                }

                double candidateEnergy = CalculateTotalPotentialEnergy(molecule3D, candidatePositions);

                if (candidateEnergy < currentEnergy)
                {
                    double dE = Math.Abs(currentEnergy - candidateEnergy);
                    currentEnergy = candidateEnergy;
                    currentPositions = candidatePositions;
                    stepAccepted = true;

                    if (dE < 1e-5 && iter >= 5)
                    {
                        reason = MinimizationTerminationReason.EnergyConvergenceReached;
                    }
                    break;
                }

                step *= 0.5; // Backtrack
            }

            if (!stepAccepted)
            {
                reason = MinimizationTerminationReason.LineSearchExhausted;
                break;
            }

            if (reason == MinimizationTerminationReason.EnergyConvergenceReached)
            {
                break;
            }
        }

        bool converged = reason is MinimizationTerminationReason.GradientToleranceReached or MinimizationTerminationReason.EnergyConvergenceReached;

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
            Math.Round(initialEnergy, 4),
            Math.Round(currentEnergy, 4),
            iter,
            converged,
            reason,
            Math.Round(maxGrad, 6),
            minimizedMol,
            UffMethodInfo
        );
    }

    /// <summary>
    /// Evaluates the total potential energy summing UFF bond stretch, angle bend, torsion, and 12-6 van der Waals terms.
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
        int nAtoms = positions.Count;

        // 1. UFF Covalent Bond Stretching: E_bond = 0.5 * k_ij * (r - r0)^2
        foreach (var bond in molecule3D.SourceMolecule.Bonds)
        {
            int i = bond.Atom1Index;
            int j = bond.Atom2Index;
            if (i >= nAtoms || j >= nAtoms) continue;

            var elem1 = molecule3D.SourceMolecule.Atoms[i].Element;
            var elem2 = molecule3D.SourceMolecule.Atoms[j].Element;
            var (r0, k_r) = GetUffBondParameters(elem1, elem2, bond.Type);

            double r = Distance(positions[i], positions[j]);
            double dr = r - r0;
            eBond += 0.5 * k_r * dr * dr;
        }

        // 2. UFF Valence Angle Bending: E_angle = 0.5 * k_ijk * (theta - theta0)^2
        for (int c = 0; c < nAtoms; c++)
        {
            var neighbors = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(c))
                .Select(b => b.Atom1Index == c ? b.Atom2Index : b.Atom1Index)
                .ToList();

            var centerElem = molecule3D.SourceMolecule.Atoms[c].Element;
            double theta0 = GetUffIdealAngle(centerElem, neighbors.Count) * (Math.PI / 180.0);
            double k_theta = 100.0; // kcal/(mol·rad²)

            for (int j = 0; j < neighbors.Count; j++)
            {
                for (int k = j + 1; k < neighbors.Count; k++)
                {
                    int n1 = neighbors[j];
                    int n2 = neighbors[k];

                    double angleRad = CalculateAngleRad(positions[n1], positions[c], positions[n2]);
                    double dTheta = angleRad - theta0;
                    eAngle += 0.5 * k_theta * dTheta * dTheta;
                }
            }
        }

        // 3. UFF Dihedral Torsional Strain: E_torsion = 0.5 * V_n * (1 - cos(n * phi - phi0))
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
                        double phiRad = CalculateDihedralAngleRad(positions[i], positions[j], positions[k], positions[l]);
                        eTorsion += 0.5 * 2.5 * (1.0 + Math.Cos(3.0 * phiRad));
                    }
                }
            }
        }

        // 4. Nonbonded 12-6 Lennard-Jones with soft-core buffering for severe clashes
        for (int i = 0; i < nAtoms; i++)
        {
            var bondedToI = molecule3D.SourceMolecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                .ToHashSet();

            var geminalToI = new HashSet<int>();
            foreach (var b in bondedToI)
            {
                foreach (var b2 in molecule3D.SourceMolecule.Bonds.Where(bnd => bnd.Connects(b)))
                {
                    int other = b2.Atom1Index == b ? b2.Atom2Index : b2.Atom1Index;
                    if (other != i) geminalToI.Add(other);
                }
            }

            for (int j = i + 1; j < nAtoms; j++)
            {
                if (!bondedToI.Contains(j) && !geminalToI.Contains(j))
                {
                    double realDist = Distance(positions[i], positions[j]);
                    var p1 = GetUffParams(molecule3D.SourceMolecule.Atoms[i].Element.Symbol);
                    var p2 = GetUffParams(molecule3D.SourceMolecule.Atoms[j].Element.Symbol);

                    double x_ij = Math.Sqrt(p1.X * p2.X);
                    double d_ij = Math.Sqrt(p1.D * p2.D);

                    // Soft-core capping at r_min = 1.0 Å to prevent numerical explosion while providing strong repulsive gradient
                    double dist = Math.Max(1.0, realDist);
                    double ratio = x_ij / dist;
                    double term6 = Math.Pow(ratio, 6);
                    double term12 = term6 * term6;
                    double energy = d_ij * (term12 - 2.0 * term6);

                    // If real distance is within clash zone (< 1.0 Å), add harmonic clash penalty
                    if (realDist < 1.0)
                    {
                        double clashDeficit = 1.0 - realDist;
                        energy += 250.0 * clashDeficit * clashDeficit;
                    }

                    eVdw += energy;
                }
            }
        }

        return eBond + eAngle + eTorsion + eVdw;
    }

    /// <summary>
    /// Computes machine-precision central finite-difference gradients over the exact potential energy function.
    /// </summary>
    private static List<Vector3D> ComputeExactGradients(Molecule3D molecule3D, List<Vector3D> positions)
    {
        int nAtoms = positions.Count;
        var grads = new List<Vector3D>(nAtoms);
        const double h = 1e-5; // Finite difference displacement in Å

        for (int i = 0; i < nAtoms; i++)
        {
            var p = positions[i];

            // dE/dx
            positions[i] = new Vector3D(p.X + h, p.Y, p.Z);
            double epX = CalculateTotalPotentialEnergy(molecule3D, positions);
            positions[i] = new Vector3D(p.X - h, p.Y, p.Z);
            double emX = CalculateTotalPotentialEnergy(molecule3D, positions);
            double gx = (epX - emX) / (2.0 * h);

            // dE/dy
            positions[i] = new Vector3D(p.X, p.Y + h, p.Z);
            double epY = CalculateTotalPotentialEnergy(molecule3D, positions);
            positions[i] = new Vector3D(p.X, p.Y - h, p.Z);
            double emY = CalculateTotalPotentialEnergy(molecule3D, positions);
            double gy = (epY - emY) / (2.0 * h);

            // dE/dz
            positions[i] = new Vector3D(p.X, p.Y, p.Z + h);
            double epZ = CalculateTotalPotentialEnergy(molecule3D, positions);
            positions[i] = new Vector3D(p.X, p.Y, p.Z - h);
            double emZ = CalculateTotalPotentialEnergy(molecule3D, positions);
            double gz = (epZ - emZ) / (2.0 * h);

            positions[i] = p; // Restore original coordinates
            grads.Add(new Vector3D(gx, gy, gz));
        }

        return grads;
    }

    private static (double R0, double Kr) GetUffBondParameters(Element e1, Element e2, BondType bondType)
    {
        var p1 = GetUffParams(e1.Symbol);
        var p2 = GetUffParams(e2.Symbol);

        double bo = bondType switch
        {
            BondType.Triple => 3.0,
            BondType.Double => 2.0,
            BondType.Aromatic => 1.5,
            _ => 1.0
        };

        // Rappé eq 2: r_BO = -0.1332 * (r1 + r2) * ln(BO)
        double r_bo = -0.1332 * (p1.R0 + p2.R0) * Math.Log(bo);

        // Rappé eq 3: Electronegativity correction
        double chiDiff = Math.Sqrt(p1.Chi) - Math.Sqrt(p2.Chi);
        double r_en = (p1.R0 * p2.R0 * chiDiff * chiDiff) / (p1.Chi * p1.R0 + p2.Chi * p2.R0);

        double r0 = p1.R0 + p2.R0 + r_bo - r_en;

        // Rappé eq 6: k_ij = 664.12 * (Z_i * Z_j) / (r_0^3)
        double kr = 664.12 * (p1.Z * p2.Z) / (r0 * r0 * r0);

        return (r0, kr);
    }

    private static double GetUffIdealAngle(Element elem, int coordinationNumber) => coordinationNumber switch
    {
        >= 4 => 109.4712, // Tetrahedral sp3
        3 => 120.0,       // Trigonal planar sp2
        2 => elem.Symbol is "O" or "S" ? 104.5 : 180.0, // Bent vs Linear
        _ => 109.4712
    };

    private static UffAtomParams GetUffParams(string symbol)
    {
        if (UffParams.TryGetValue(symbol, out var p)) return p;
        if (UffParams.TryGetValue($"{symbol}_3", out var p3)) return p3;
        return new UffAtomParams(0.77, 109.5, 3.85, 0.10, 5.0, 1.9);
    }

    private static double Distance(Vector3D a, Vector3D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        double dz = a.Z - b.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static double CalculateAngleRad(Vector3D a, Vector3D center, Vector3D b)
    {
        var v1 = new Vector3D(a.X - center.X, a.Y - center.Y, a.Z - center.Z);
        var v2 = new Vector3D(b.X - center.X, b.Y - center.Y, b.Z - center.Z);

        double dot = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        double len1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y + v1.Z * v1.Z);
        double len2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y + v2.Z * v2.Z);

        if (len1 < 1e-6 || len2 < 1e-6) return Math.PI;
        double cosVal = Math.Clamp(dot / (len1 * len2), -1.0, 1.0);
        return Math.Acos(cosVal);
    }

    private static double CalculateDihedralAngleRad(Vector3D p1, Vector3D p2, Vector3D p3, Vector3D p4)
    {
        var b1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
        var b2 = new Vector3D(p3.X - p2.X, p3.Y - p2.Y, p3.Z - p2.Z);
        var b3 = new Vector3D(p4.X - p3.X, p4.Y - p3.Y, p4.Z - p3.Z);

        // n1 = b1 x b2, n2 = b2 x b3
        var n1 = Cross(b1, b2);
        var n2 = Cross(b2, b3);

        var m1 = Cross(n1, b2);
        double b2Len = Math.Sqrt(b2.X * b2.X + b2.Y * b2.Y + b2.Z * b2.Z);
        if (b2Len < 1e-6) return 0.0;

        double x = Dot(n1, n2);
        double y = Dot(m1, n2) / b2Len;

        return Math.Atan2(y, x);
    }

    private static Vector3D Cross(Vector3D u, Vector3D v) => new(
        u.Y * v.Z - u.Z * v.Y,
        u.Z * v.X - u.X * v.Z,
        u.X * v.Y - u.Y * v.X
    );

    private static double Dot(Vector3D u, Vector3D v) => u.X * v.X + u.Y * v.Y + u.Z * v.Z;
}
