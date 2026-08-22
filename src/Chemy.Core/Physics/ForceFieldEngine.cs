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

/// <summary>Auditable decomposition of the implemented force-field potential.</summary>
public readonly record struct ForceFieldEnergyComponents(
    double BondStretchKcalPerMol,
    double AngleBendKcalPerMol,
    double TorsionKcalPerMol,
    double InversionKcalPerMol,
    double VanDerWaalsKcalPerMol)
{
    /// <summary>Total potential energy in kcal/mol.</summary>
    public double TotalKcalPerMol => BondStretchKcalPerMol + AngleBendKcalPerMol +
        TorsionKcalPerMol + InversionKcalPerMol + VanDerWaalsKcalPerMol;
}

/// <summary>
/// UFF-inspired molecular mechanics engine implementing a documented subset of the potential described by
/// Rappé, Casewit, Colwell, Goddard &amp; Skiff (J. Am. Chem. Soc. 1992, 114, 10024-10035).
/// It is not a drop-in or numerical-equivalence implementation of RDKit UFF.
/// </summary>
public static class ForceFieldEngine
{
    private static readonly ScientificMethodInfo UffMethodInfo = new(
        "UFF-Inspired Classical Molecular Mechanics Potential (Rappé et al. 1992)",
        "1992.1",
        EvidenceLevel.NumericalApproximation,
        "Organic small molecules containing H, C, N, O, P, S, F, Cl, Br, I.",
        [
            "Evaluates UFF bond stretch, valence angle bend, dihedral torsion (3-fold & 2-fold conjugated), out-of-plane inversion, and buffered 12-6 Lennard-Jones nonbonded terms.",
            "Uses central finite-difference gradients with bounded-memory L-BFGS and an Armijo line search; does not model electrostatic point charges or non-linear Fourier angle potentials."
        ]
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
        ["N_1"] = new(0.656, 180.0, 3.660, 0.069, 6.899, 2.544),
        ["O_3"] = new(0.658, 104.51, 3.500, 0.060, 8.741, 2.300),
        ["O_2"] = new(0.634, 120.0, 3.500, 0.060, 8.741, 2.300),
        ["O_R"] = new(0.658, 120.0, 3.500, 0.060, 8.741, 2.300),
        ["F"] = new(0.668, 180.0, 3.364, 0.050, 10.874, 1.960),
        ["Cl"] = new(1.044, 180.0, 3.947, 0.227, 8.564, 2.668),
        ["Br"] = new(1.192, 180.0, 4.189, 0.251, 7.790, 3.328),
        ["I"] = new(1.382, 180.0, 4.542, 0.339, 6.802, 4.280),
        ["S_3"] = new(1.064, 103.2, 4.035, 0.274, 6.928, 2.766),
        ["S_2"] = new(0.999, 120.0, 4.035, 0.274, 6.928, 2.766),
        ["S_R"] = new(1.049, 120.0, 4.035, 0.274, 6.928, 2.766),
        ["P_3"] = new(1.100, 93.3, 4.147, 0.305, 5.463, 2.895)
    };

    private readonly record struct BondParam(int Atom1, int Atom2, double R0, double Kr);
    private readonly record struct AngleParam(int Center, int N1, int N2, double Theta0Rad, double KTheta);
    private readonly record struct TorsionParam(int J, int K, int N1, int N2, double BarrierPerPair, bool IsSp2);
    private readonly record struct InversionParam(int Center, int Axis, int N1, int N2, double KInvPerPermutation);
    private readonly record struct VdwParam(int Atom1, int Atom2, double Xij, double Dij);

    private sealed class TopologyParams
    {
        public required BondParam[] Bonds { get; init; }
        public required AngleParam[] Angles { get; init; }
        public required TorsionParam[] Torsions { get; init; }
        public required InversionParam[] Inversions { get; init; }
        public required VdwParam[] Vdws { get; init; }
    }

    private static TopologyParams PrecomputeTopology(Molecule molecule, int nAtoms)
    {
        var atomTypes = new (string TypeName, double IdealAngleDeg, double R0)[nAtoms];
        var uffParams = new UffAtomParams[nAtoms];
        for (int i = 0; i < nAtoms; i++)
        {
            atomTypes[i] = GetUffAtomType(molecule, i);
            uffParams[i] = GetUffParams(atomTypes[i].TypeName);
        }

        // 1. Bonds
        var bondList = new List<BondParam>(molecule.Bonds.Count);
        foreach (var bond in molecule.Bonds)
        {
            int i = bond.Atom1Index;
            int j = bond.Atom2Index;
            if (i >= nAtoms || j >= nAtoms) continue;

            var (r0, kr) = GetUffBondParametersFromParams(uffParams[i], uffParams[j], bond.Type);
            bondList.Add(new BondParam(i, j, r0, kr));
        }

        // 2. Angles
        var angleList = new List<AngleParam>();
        for (int c = 0; c < nAtoms; c++)
        {
            var neighbors = molecule.Bonds
                .Where(b => b.Connects(c))
                .Select(b => b.Atom1Index == c ? b.Atom2Index : b.Atom1Index)
                .ToList();

            double theta0 = atomTypes[c].IdealAngleDeg * (Math.PI / 180.0);
            double k_theta = 100.0;

            for (int j = 0; j < neighbors.Count; j++)
            {
                for (int k = j + 1; k < neighbors.Count; k++)
                {
                    angleList.Add(new AngleParam(c, neighbors[j], neighbors[k], theta0, k_theta));
                }
            }
        }

        // 3. Torsions
        var torsionList = new List<TorsionParam>();
        foreach (var centralBond in molecule.Bonds)
        {
            int j = centralBond.Atom1Index;
            int k = centralBond.Atom2Index;
            if (j >= nAtoms || k >= nAtoms) continue;

            var jNeighbors = molecule.Bonds
                .Where(b => b.Connects(j))
                .Select(b => b.Atom1Index == j ? b.Atom2Index : b.Atom1Index)
                .Where(n => n != k)
                .ToList();

            var kNeighbors = molecule.Bonds
                .Where(b => b.Connects(k))
                .Select(b => b.Atom1Index == k ? b.Atom2Index : b.Atom1Index)
                .Where(n => n != j)
                .ToList();

            int nPairs = jNeighbors.Count * kNeighbors.Count;
            if (nPairs == 0) continue;

            var typeJ = atomTypes[j];
            var typeK = atomTypes[k];

            bool jIsSp2 = typeJ.TypeName is "C_2" or "C_R" or "N_2" or "N_R" or "O_2" or "S_2" or "S_R";
            bool kIsSp2 = typeK.TypeName is "C_2" or "C_R" or "N_2" or "N_R" or "O_2" or "S_2" or "S_R";

            if (centralBond.Type == BondType.Double || (jIsSp2 && kIsSp2))
            {
                double vBarrier = centralBond.Type == BondType.Double ? 45.0 : 5.0;
                double vDouble = vBarrier / nPairs;
                foreach (var i in jNeighbors)
                {
                    foreach (var l in kNeighbors)
                    {
                        if (i != l && i < nAtoms && l < nAtoms)
                        {
                            torsionList.Add(new TorsionParam(j, k, i, l, vDouble, true));
                        }
                    }
                }
            }
            else
            {
                double vPerPair = 2.5 / nPairs;
                foreach (var i in jNeighbors)
                {
                    foreach (var l in kNeighbors)
                    {
                        if (i != l && i < nAtoms && l < nAtoms)
                        {
                            torsionList.Add(new TorsionParam(j, k, i, l, vPerPair, false));
                        }
                    }
                }
            }
        }

        // 4. UFF Inversion (out-of-plane) terms for trivalent sp2 centers (Rappé et al. 1992 §II.C)
        var inversionList = new List<InversionParam>();
        for (int c = 0; c < nAtoms; c++)
        {
            var neighbors = molecule.Bonds
                .Where(b => b.Connects(c))
                .Select(b => b.Atom1Index == c ? b.Atom2Index : b.Atom1Index)
                .ToList();

            if (neighbors.Count == 3)
            {
                var type = atomTypes[c];
                if (type.TypeName is "C_2" or "C_R" or "N_2" or "N_R" or "O_2")
                {
                    bool isCarbonylCarbon = type.TypeName == "C_2" && molecule.Bonds.Any(bond =>
                        bond.Connects(c) &&
                        bond.Type == BondType.Double &&
                        molecule.Atoms[bond.Atom1Index == c ? bond.Atom2Index : bond.Atom1Index].Element.Symbol == "O");
                    double totalForceConstant = isCarbonylCarbon ? 50.0 : 6.0;
                    double kInvPerPermutation = totalForceConstant / 3.0;
                    inversionList.Add(new InversionParam(c, neighbors[0], neighbors[1], neighbors[2], kInvPerPermutation));
                    inversionList.Add(new InversionParam(c, neighbors[1], neighbors[0], neighbors[2], kInvPerPermutation));
                    inversionList.Add(new InversionParam(c, neighbors[2], neighbors[0], neighbors[1], kInvPerPermutation));
                }
            }
        }

        // 5. VdW nonbonded pairs (1,4+)
        var vdwList = new List<VdwParam>();
        for (int i = 0; i < nAtoms; i++)
        {
            var bondedToI = molecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                .ToHashSet();

            var geminalToI = new HashSet<int>();
            foreach (var b in bondedToI)
            {
                foreach (var b2 in molecule.Bonds.Where(bnd => bnd.Connects(b)))
                {
                    int other = b2.Atom1Index == b ? b2.Atom2Index : b2.Atom1Index;
                    if (other != i) geminalToI.Add(other);
                }
            }

            for (int j = i + 1; j < nAtoms; j++)
            {
                if (!bondedToI.Contains(j) && !geminalToI.Contains(j))
                {
                    var p1 = uffParams[i];
                    var p2 = uffParams[j];
                    double x_ij = Math.Sqrt(p1.X * p2.X);
                    double d_ij = Math.Sqrt(p1.D * p2.D);
                    vdwList.Add(new VdwParam(i, j, x_ij, d_ij));
                }
            }
        }

        return new TopologyParams
        {
            Bonds = [.. bondList],
            Angles = [.. angleList],
            Torsions = [.. torsionList],
            Inversions = [.. inversionList],
            Vdws = [.. vdwList]
        };
    }

    /// <summary>
    /// Performs molecular mechanics geometric relaxation of a 3D molecule.
    /// </summary>
    public static EnergyMinimizationResult MinimizeEnergy(
        Molecule3D molecule3D,
        int maxIterations = 500,
        double gradientTolerance = 1e-3)
    {
        ArgumentNullException.ThrowIfNull(molecule3D);
        if (maxIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxIterations), maxIterations, "Iteration budget must be positive.");
        }
        if (!double.IsFinite(gradientTolerance) || gradientTolerance <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gradientTolerance), gradientTolerance, "Gradient tolerance must be a finite positive value.");
        }

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

        if (molecule3D.SourceMolecule != null && !molecule3D.SourceMolecule.HasBondedTopology)
        {
            throw new InvalidOperationException(
                $"Molecule '{molecule3D.Name}' has no bonded topology. Force field energy minimization requires a bonded molecular structure, not an empirical formula without connectivity.");
        }

        var sourceMol = molecule3D.SourceMolecule ?? new Molecule(molecule3D.Name, molecule3D.Atoms.Select(a => a.Atom), Enumerable.Empty<Bond>());
        var topo = PrecomputeTopology(sourceMol, nAtoms);

        var currentPositions = new Vector3D[nAtoms];
        for (int i = 0; i < nAtoms; i++) currentPositions[i] = molecule3D.Atoms[i].Position;

        double currentEnergy = CalculateTotalPotentialEnergyFast(topo, currentPositions);
        double initialEnergy = currentEnergy;

        var reason = MinimizationTerminationReason.MaximumIterationsReached;
        int iterationsPerformed = 0;
        var gradients = ComputeExactGradientsFast(topo, currentPositions);
        double maxGrad = MaxAbsComponent(gradients);

        // Limited-memory BFGS avoids the pathological slow-down of steepest descent on the
        // strongly anisotropic bond/angle surface while keeping memory bounded for large molecules.
        const int historyLimit = 7;
        var displacementHistory = new List<Vector3D[]>(historyLimit);
        var gradientDeltaHistory = new List<Vector3D[]>(historyLimit);
        var inverseCurvatureHistory = new List<double>(historyLimit);

        for (int iter = 0; iter < maxIterations; iter++)
        {
            if (maxGrad < gradientTolerance)
            {
                reason = MinimizationTerminationReason.GradientToleranceReached;
                break;
            }

            var direction = ComputeLbfgsDirection(
                gradients,
                displacementHistory,
                gradientDeltaHistory,
                inverseCurvatureHistory);
            double directionalDerivative = Dot(gradients, direction);

            // Numerical noise or non-smooth soft-core boundaries can invalidate the quasi-Newton
            // direction. Restart safely with steepest descent rather than accepting an uphill step.
            if (!double.IsFinite(directionalDerivative) || directionalDerivative >= 0.0)
            {
                displacementHistory.Clear();
                gradientDeltaHistory.Clear();
                inverseCurvatureHistory.Clear();
                direction = Scale(gradients, -1.0);
                directionalDerivative = -Dot(gradients, gradients);
            }

            double maxDirection = MaxAbsComponent(direction);
            double step = displacementHistory.Count == 0
                ? Math.Min(1.0, 0.10 / Math.Max(maxDirection, 1e-12))
                : 1.0;
            bool stepAccepted = false;
            Vector3D[]? candidatePositions = null;
            double candidateEnergy = double.PositiveInfinity;

            // Armijo sufficient-decrease line search gives deterministic monotonic energy descent.
            for (int lineIter = 0; lineIter < 30; lineIter++)
            {
                candidatePositions = AddScaled(currentPositions, direction, step);
                candidateEnergy = CalculateTotalPotentialEnergyFast(topo, candidatePositions);
                if (double.IsFinite(candidateEnergy) &&
                    candidateEnergy <= currentEnergy + (1e-4 * step * directionalDerivative))
                {
                    stepAccepted = true;
                    break;
                }

                step *= 0.5;
            }

            if (!stepAccepted || candidatePositions is null)
            {
                reason = MinimizationTerminationReason.LineSearchExhausted;
                break;
            }

            var candidateGradients = ComputeExactGradientsFast(topo, candidatePositions);
            var displacement = Difference(candidatePositions, currentPositions);
            var gradientDelta = Difference(candidateGradients, gradients);
            double curvature = Dot(displacement, gradientDelta);
            double curvatureScale = Math.Sqrt(Dot(displacement, displacement) * Dot(gradientDelta, gradientDelta));

            if (double.IsFinite(curvature) && curvature > 1e-10 * Math.Max(1.0, curvatureScale))
            {
                if (displacementHistory.Count == historyLimit)
                {
                    displacementHistory.RemoveAt(0);
                    gradientDeltaHistory.RemoveAt(0);
                    inverseCurvatureHistory.RemoveAt(0);
                }

                displacementHistory.Add(displacement);
                gradientDeltaHistory.Add(gradientDelta);
                inverseCurvatureHistory.Add(1.0 / curvature);
            }

            double energyChange = Math.Abs(currentEnergy - candidateEnergy);
            currentPositions = candidatePositions;
            currentEnergy = candidateEnergy;
            gradients = candidateGradients;
            maxGrad = MaxAbsComponent(gradients);
            iterationsPerformed = iter + 1;

            if (maxGrad < gradientTolerance)
            {
                reason = MinimizationTerminationReason.GradientToleranceReached;
                break;
            }

            // Energy convergence is accepted only near a stationary point. This prevents a tiny
            // rejected step from being mislabeled as convergence while the gradient remains large.
            double energyThreshold = 1e-10 * Math.Max(1.0, Math.Abs(currentEnergy));
            if (energyChange <= energyThreshold && maxGrad < Math.Max(gradientTolerance * 10.0, 1e-4))
            {
                reason = MinimizationTerminationReason.EnergyConvergenceReached;
                break;
            }
        }

        bool converged = reason is MinimizationTerminationReason.GradientToleranceReached or MinimizationTerminationReason.EnergyConvergenceReached;

        var minimizedAtoms = new Atom3D[nAtoms];
        for (int i = 0; i < nAtoms; i++)
        {
            minimizedAtoms[i] = new Atom3D(molecule3D.Atoms[i].Atom, currentPositions[i]);
        }

        var minimizedMol = new Molecule3D(
            molecule3D.Name,
            molecule3D.ChemicalFormula,
            molecule3D.VseprShape,
            molecule3D.IdealBondAngleDegrees,
            minimizedAtoms,
            molecule3D.SourceMolecule ?? new Molecule(molecule3D.Name, molecule3D.Atoms.Select(a => a.Atom), Enumerable.Empty<Bond>())
        );

        return new EnergyMinimizationResult(
            molecule3D.ChemicalFormula,
            initialEnergy,
            currentEnergy,
            iterationsPerformed,
            converged,
            reason,
            maxGrad,
            minimizedMol,
            UffMethodInfo
        );
    }

    private static Vector3D[] ComputeLbfgsDirection(
        Vector3D[] gradient,
        IReadOnlyList<Vector3D[]> displacementHistory,
        IReadOnlyList<Vector3D[]> gradientDeltaHistory,
        IReadOnlyList<double> inverseCurvatureHistory)
    {
        var q = (Vector3D[])gradient.Clone();
        var alpha = new double[displacementHistory.Count];

        for (int i = displacementHistory.Count - 1; i >= 0; i--)
        {
            alpha[i] = inverseCurvatureHistory[i] * Dot(displacementHistory[i], q);
            q = AddScaled(q, gradientDeltaHistory[i], -alpha[i]);
        }

        double scale = 1.0;
        if (displacementHistory.Count > 0)
        {
            int last = displacementHistory.Count - 1;
            double sy = Dot(displacementHistory[last], gradientDeltaHistory[last]);
            double yy = Dot(gradientDeltaHistory[last], gradientDeltaHistory[last]);
            if (sy > 0.0 && yy > 0.0)
            {
                scale = sy / yy;
            }
        }

        var result = Scale(q, scale);
        for (int i = 0; i < displacementHistory.Count; i++)
        {
            double beta = inverseCurvatureHistory[i] * Dot(gradientDeltaHistory[i], result);
            result = AddScaled(result, displacementHistory[i], alpha[i] - beta);
        }

        return Scale(result, -1.0);
    }

    private static Vector3D[] AddScaled(IReadOnlyList<Vector3D> left, IReadOnlyList<Vector3D> right, double scale)
    {
        var result = new Vector3D[left.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Vector3D(
                left[i].X + scale * right[i].X,
                left[i].Y + scale * right[i].Y,
                left[i].Z + scale * right[i].Z);
        }
        return result;
    }

    private static Vector3D[] Difference(IReadOnlyList<Vector3D> left, IReadOnlyList<Vector3D> right) =>
        AddScaled(left, right, -1.0);

    private static Vector3D[] Scale(IReadOnlyList<Vector3D> values, double scale)
    {
        var result = new Vector3D[values.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Vector3D(values[i].X * scale, values[i].Y * scale, values[i].Z * scale);
        }
        return result;
    }

    private static double Dot(IReadOnlyList<Vector3D> left, IReadOnlyList<Vector3D> right)
    {
        double result = 0.0;
        for (int i = 0; i < left.Count; i++)
        {
            result += left[i].X * right[i].X + left[i].Y * right[i].Y + left[i].Z * right[i].Z;
        }
        return result;
    }

    private static double MaxAbsComponent(IReadOnlyList<Vector3D> values)
    {
        double max = 0.0;
        for (int i = 0; i < values.Count; i++)
        {
            double candidate = Math.Max(Math.Abs(values[i].X), Math.Max(Math.Abs(values[i].Y), Math.Abs(values[i].Z)));
            if (candidate > max) max = candidate;
        }
        return max;
    }

    /// <summary>
    /// Evaluates the total five-term potential energy in kcal/mol.
    /// </summary>
    public static double CalculateTotalEnergy(Molecule3D molecule3D)
    {
        ArgumentNullException.ThrowIfNull(molecule3D);
        int nAtoms = molecule3D.Atoms.Count;
        var sourceMol = molecule3D.SourceMolecule ?? new Molecule(molecule3D.Name, molecule3D.Atoms.Select(a => a.Atom), Enumerable.Empty<Bond>());
        var topo = PrecomputeTopology(sourceMol, nAtoms);
        var positions = new Vector3D[nAtoms];
        for (int i = 0; i < nAtoms; i++) positions[i] = molecule3D.Atoms[i].Position;
        return CalculateTotalPotentialEnergyFast(topo, positions);
    }

    /// <summary>
    /// Evaluates an auditable decomposition of bond, angle, torsion, inversion, and van der Waals energy terms.
    /// </summary>
    public static ForceFieldEnergyComponents CalculateEnergyComponents(Molecule3D molecule3D)
    {
        ArgumentNullException.ThrowIfNull(molecule3D);
        int nAtoms = molecule3D.Atoms.Count;
        var sourceMol = molecule3D.SourceMolecule ?? new Molecule(molecule3D.Name, molecule3D.Atoms.Select(a => a.Atom), Enumerable.Empty<Bond>());
        var topo = PrecomputeTopology(sourceMol, nAtoms);
        var positions = new Vector3D[nAtoms];
        for (int i = 0; i < nAtoms; i++) positions[i] = molecule3D.Atoms[i].Position;
        return CalculateEnergyComponentsFast(topo, positions);
    }

    private static double CalculateTotalPotentialEnergyFast(TopologyParams topo, Vector3D[] positions) =>
        CalculateEnergyComponentsFast(topo, positions).TotalKcalPerMol;

    private static ForceFieldEnergyComponents CalculateEnergyComponentsFast(TopologyParams topo, Vector3D[] positions)
    {
        double eBond = 0.0;
        double eAngle = 0.0;
        double eTorsion = 0.0;
        double eVdw = 0.0;

        // 1. UFF Covalent Bond Stretching
        var bonds = topo.Bonds;
        for (int b = 0; b < bonds.Length; b++)
        {
            ref readonly var bond = ref bonds[b];
            double r = Distance(positions[bond.Atom1], positions[bond.Atom2]);
            double dr = r - bond.R0;
            eBond += 0.5 * bond.Kr * dr * dr;
        }

        // 2. UFF Valence Angle Bending
        var angles = topo.Angles;
        for (int a = 0; a < angles.Length; a++)
        {
            ref readonly var angle = ref angles[a];
            double angleRad = CalculateAngleRad(positions[angle.N1], positions[angle.Center], positions[angle.N2]);
            double dTheta = angleRad - angle.Theta0Rad;
            eAngle += 0.5 * angle.KTheta * dTheta * dTheta;
        }

        // 3. UFF Dihedral Torsional Strain
        var torsions = topo.Torsions;
        for (int t = 0; t < torsions.Length; t++)
        {
            ref readonly var tor = ref torsions[t];
            double phiRad = CalculateDihedralAngleRad(positions[tor.N1], positions[tor.J], positions[tor.K], positions[tor.N2]);
            if (tor.IsSp2)
            {
                eTorsion += 0.5 * tor.BarrierPerPair * (1.0 - Math.Cos(2.0 * phiRad));
            }
            else
            {
                eTorsion += 0.5 * tor.BarrierPerPair * (1.0 + Math.Cos(3.0 * phiRad));
            }
        }

        // 4. Nonbonded 12-6 Lennard-Jones with soft-core buffering
        var vdws = topo.Vdws;
        for (int v = 0; v < vdws.Length; v++)
        {
            ref readonly var vdw = ref vdws[v];
            double realDist = Distance(positions[vdw.Atom1], positions[vdw.Atom2]);
            double dist = Math.Max(1.0, realDist);
            double ratio = vdw.Xij / dist;
            double term6 = Math.Pow(ratio, 6);
            double term12 = term6 * term6;
            double energy = vdw.Dij * (term12 - 2.0 * term6);

            if (realDist < 1.0)
            {
                double clashDeficit = 1.0 - realDist;
                energy += 250.0 * clashDeficit * clashDeficit;
            }

            eVdw += energy;
        }

        // 5. UFF Inversion (out-of-plane): E_inv = (K_inv / 3) * (1 - cos(omega)) (Rappé et al. 1992 §II.C eq 17)
        double eInversion = 0.0;
        var invs = topo.Inversions;
        for (int inv = 0; inv < invs.Length; inv++)
        {
            ref readonly var item = ref invs[inv];
            var vAxis = new Vector3D(positions[item.Axis].X - positions[item.Center].X, positions[item.Axis].Y - positions[item.Center].Y, positions[item.Axis].Z - positions[item.Center].Z);
            var v1 = new Vector3D(positions[item.N1].X - positions[item.Center].X, positions[item.N1].Y - positions[item.Center].Y, positions[item.N1].Z - positions[item.Center].Z);
            var v2 = new Vector3D(positions[item.N2].X - positions[item.Center].X, positions[item.N2].Y - positions[item.Center].Y, positions[item.N2].Z - positions[item.Center].Z);

            var normPlane = Normalize(Cross(v1, v2));
            double lenAxis = Math.Sqrt(vAxis.X * vAxis.X + vAxis.Y * vAxis.Y + vAxis.Z * vAxis.Z);
            if (lenAxis > 1e-6)
            {
                double sinOmega = (vAxis.X * normPlane.X + vAxis.Y * normPlane.Y + vAxis.Z * normPlane.Z) / lenAxis;
                sinOmega = Math.Clamp(sinOmega, -1.0, 1.0);
                double cosOmega = Math.Sqrt(Math.Max(0.0, 1.0 - sinOmega * sinOmega));
                eInversion += item.KInvPerPermutation * (1.0 - cosOmega);
            }
        }

        return new ForceFieldEnergyComponents(eBond, eAngle, eTorsion, eInversion, eVdw);
    }

    /// <summary>
    /// Determines the UFF atom type, ideal equilibrium angle, and covalent single-bond radius based on
    /// bonding topology, coordination number, aromaticity, and chemical hybridization environment.
    /// </summary>
    public static (string TypeName, double IdealAngleDeg, double R0) GetUffAtomType(Molecule molecule, int atomIndex)
    {
        var atom = molecule.Atoms[atomIndex];
        var symbol = atom.Element.Symbol;
        var bonds = molecule.Bonds.Where(b => b.Connects(atomIndex)).ToList();
        int coord = bonds.Count;

        bool hasAromatic = bonds.Any(b => b.Type == BondType.Aromatic);
        bool hasTriple = bonds.Any(b => b.Type == BondType.Triple);
        int doubleCount = bonds.Count(b => b.Type == BondType.Double);
        bool hasDouble = doubleCount > 0;

        switch (symbol)
        {
            case "H":
                return ("H", 180.0, 0.354);

            case "C":
                if (hasAromatic) return ("C_R", 120.0, 0.729);
                if (hasTriple || doubleCount >= 2) return ("C_1", 180.0, 0.706);
                if (hasDouble) return ("C_2", 120.0, 0.732);
                return ("C_3", 109.4712, 0.757);

            case "N":
                if (hasAromatic) return ("N_R", 120.0, 0.699);
                if (hasTriple) return ("N_1", 180.0, 0.656);
                if (hasDouble) return ("N_2", 120.0, 0.685);

                // Planar amide / conjugated / resonant environment detection:
                // If Nitrogen is connected to a carbon (or S/P) that is double-bonded to an electronegative atom (O, S, N) or in an aromatic ring
                bool isResonantPlanar = false;
                foreach (var b in bonds)
                {
                    int neighbor = b.Atom1Index == atomIndex ? b.Atom2Index : b.Atom1Index;
                    var neighborAtom = molecule.Atoms[neighbor];
                    if (neighborAtom.Element.Symbol is "C" or "S" or "P")
                    {
                        bool neighborHasDoubleToElectronegative = molecule.Bonds.Any(nb =>
                            nb.Connects(neighbor) &&
                            nb.Type == BondType.Double &&
                            molecule.Atoms[nb.Atom1Index == neighbor ? nb.Atom2Index : nb.Atom1Index].Element.Symbol is "O" or "S" or "N");
                        bool neighborIsAromatic = molecule.Bonds.Any(nb => nb.Connects(neighbor) && nb.Type == BondType.Aromatic);
                        if (neighborHasDoubleToElectronegative || neighborIsAromatic)
                        {
                            isResonantPlanar = true;
                            break;
                        }
                    }
                }

                if (isResonantPlanar)
                {
                    return ("N_2", 120.0, 0.685); // sp2 planar amide / resonant nitrogen
                }

                if (coord >= 4)
                {
                    return ("N_3", 109.4712, 0.700); // sp3 tetrahedral ammonium
                }

                return ("N_3", 106.70, 0.700); // sp3 pyramidal amine / ammonia

            case "O":
                if (hasAromatic) return ("O_R", 120.0, 0.658);
                if (hasDouble) return ("O_2", 120.0, 0.634);
                return ("O_3", 104.51, 0.658);

            case "P":
                if (coord >= 4) return ("P_3", 109.4712, 1.100);
                return ("P_3", 93.30, 1.100); // sp3 pyramidal phosphine

            case "S":
                if (hasAromatic) return ("S_R", 120.0, 1.049);
                if (hasDouble) return ("S_2", 120.0, 0.999);
                if (coord == 2 && bonds.All(b => molecule.Atoms[b.Atom1Index == atomIndex ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H"))
                {
                    return ("S_3", 92.10, 1.064); // H2S bent
                }
                return ("S_3", 103.20, 1.064);

            case "F":
                return ("F", 180.0, 0.668);

            case "Cl":
                return ("Cl", 180.0, 1.044);

            case "Br":
                return ("Br", 180.0, 1.192);

            case "I":
                return ("I", 180.0, 1.382);

            default:
                return (symbol, 109.4712, 0.770);
        }
    }

    /// <summary>
    /// Computes machine-precision central finite-difference gradients over the exact potential energy function.
    /// </summary>
    private static Vector3D[] ComputeExactGradientsFast(TopologyParams topo, Vector3D[] positions)
    {
        int nAtoms = positions.Length;
        var grads = new Vector3D[nAtoms];
        const double h = 1e-5; // Finite difference displacement in Å

        for (int i = 0; i < nAtoms; i++)
        {
            var p = positions[i];

            // dE/dx
            positions[i] = new Vector3D(p.X + h, p.Y, p.Z);
            double epX = CalculateTotalPotentialEnergyFast(topo, positions);
            positions[i] = new Vector3D(p.X - h, p.Y, p.Z);
            double emX = CalculateTotalPotentialEnergyFast(topo, positions);
            double gx = (epX - emX) / (2.0 * h);

            // dE/dy
            positions[i] = new Vector3D(p.X, p.Y + h, p.Z);
            double epY = CalculateTotalPotentialEnergyFast(topo, positions);
            positions[i] = new Vector3D(p.X, p.Y - h, p.Z);
            double emY = CalculateTotalPotentialEnergyFast(topo, positions);
            double gy = (epY - emY) / (2.0 * h);

            // dE/dz
            positions[i] = new Vector3D(p.X, p.Y, p.Z + h);
            double epZ = CalculateTotalPotentialEnergyFast(topo, positions);
            positions[i] = new Vector3D(p.X, p.Y, p.Z - h);
            double emZ = CalculateTotalPotentialEnergyFast(topo, positions);
            double gz = (epZ - emZ) / (2.0 * h);

            positions[i] = p; // Restore original coordinates
            grads[i] = new Vector3D(gx, gy, gz);
        }

        return grads;
    }

    private static (double R0, double Kr) GetUffBondParameters(Molecule molecule, int atom1Idx, int atom2Idx, BondType bondType)
    {
        var type1 = GetUffAtomType(molecule, atom1Idx);
        var type2 = GetUffAtomType(molecule, atom2Idx);
        var p1 = GetUffParams(type1.TypeName);
        var p2 = GetUffParams(type2.TypeName);
        return GetUffBondParametersFromParams(p1, p2, bondType);
    }

    private static (double R0, double Kr) GetUffBondParametersFromParams(UffAtomParams p1, UffAtomParams p2, BondType bondType)
    {
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

    private static UffAtomParams GetUffParams(string typeName)
    {
        if (UffParams.TryGetValue(typeName, out var p)) return p;
        if (UffParams.TryGetValue($"{typeName}_3", out var p3)) return p3;
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

    private static Vector3D Normalize(Vector3D v)
    {
        double len = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return len < 1e-6 ? new Vector3D(0, 0, 1) : new Vector3D(v.X / len, v.Y / len, v.Z / len);
    }
}
