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
)
{
    public ScientificApplicabilityAssessment Applicability { get; init; } = new(
        ApplicabilityStatus.OutOfDomain,
        ["Applicability was not evaluated."]);
}

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

/// <summary>Auditable Cartesian derivative of the UFF potential.</summary>
/// <param name="CartesianGradientKcalPerMolAngstrom">Per-atom dE/d(x,y,z), in kcal/(mol·Å), in molecule atom order.</param>
/// <param name="FiniteDifferenceStepAngstrom">Central-difference displacement used for every Cartesian component.</param>
/// <param name="MaxAbsComponentKcalPerMolAngstrom">Infinity norm of the returned Cartesian gradient.</param>
/// <param name="MethodInfo">Scientific method provenance and applicability metadata.</param>
public sealed record ForceFieldGradientResult(
    IReadOnlyList<Vector3D> CartesianGradientKcalPerMolAngstrom,
    double FiniteDifferenceStepAngstrom,
    double MaxAbsComponentKcalPerMolAngstrom,
    ScientificMethodInfo MethodInfo)
{
    public ScientificApplicabilityAssessment Applicability { get; init; } = new(
        ApplicabilityStatus.OutOfDomain,
        ["Applicability was not evaluated."]);
}

/// <summary>
/// UFF-compatible molecular mechanics engine implementing the published potential for a declared organic subset described by
/// Rappé, Casewit, Colwell, Goddard &amp; Skiff (J. Am. Chem. Soc. 1992, 114, 10024-10035).
/// Unsupported atom types fail closed instead of receiving generic fallback parameters.
/// </summary>
public static class ForceFieldEngine
{
    private static readonly ScientificMethodInfo UffMethodInfo = new(
        "UFF-Compatible Organic-Subset Molecular Mechanics (Rappé et al. 1992)",
        "1992.2",
        EvidenceLevel.EmpiricalModel,
        "Covalently bonded organic molecules composed of H, C, N, O, P, S, F, Cl, Br, and I using the explicitly implemented UFF atom types.",
        [
            "Evaluates UFF bond stretch, Fourier valence-angle bend, typed dihedral torsion, out-of-plane inversion, and geometric-mean 12-6 Lennard-Jones terms.",
            "Uses central finite-difference gradients with bounded-memory L-BFGS and an Armijo line search; atom types outside the declared subset are rejected."
        ]
    )
    {
        ReferenceUris =
        [
            "https://doi.org/10.1021/ja00051a040",
            "https://github.com/rdkit/rdkit/tree/master/Code/ForceField/UFF"
        ],
        ValidationEvidence = new ScientificValidationEvidence(
            "rdkit-uff-fixed-coordinate-gradient-geometry-v2.8",
            "2.8",
            24,
            [
                new("FixedCoordinateEnergyMaximumAbsoluteError", 0.0001, "kcal/mol"),
                new("CartesianGradientMaximumAbsoluteError", 0.0005, "kcal/(mol*angstrom)"),
                new("OptimizedPairwiseDistanceRmsError", 0.002, "angstrom")
            ],
            "src/Chemy.Core.Tests/ValidationData/rdkit_uff_butane_reference.json",
            "0d866e07e7e4ddc6c3fdc6fc28858b65e60c570fcf1b60947645b399d846b4e5",
            false,
            false)
    };

    private record UffAtomParams(
        double R0,
        double Theta0Deg,
        double X,
        double D,
        double Chi,
        double Z,
        double Sp3TorsionBarrier,
        double Sp2TorsionParameter);

    // Rappé et al. (1992) Table 1 Parameters
    private static readonly Dictionary<string, UffAtomParams> UffParams = new(StringComparer.OrdinalIgnoreCase)
    {
        ["H"] = new(0.354, 180.0, 2.886, 0.044, 4.528, 0.712, 0.0, 0.0),
        ["C_3"] = new(0.757, 109.47, 3.851, 0.105, 5.343, 1.912, 2.119, 2.0),
        ["C_2"] = new(0.732, 120.0, 3.851, 0.105, 5.343, 1.912, 0.0, 2.0),
        ["C_R"] = new(0.729, 120.0, 3.851, 0.105, 5.343, 1.912, 0.0, 2.0),
        ["C_1"] = new(0.706, 180.0, 3.851, 0.105, 5.343, 1.912, 0.0, 2.0),
        ["N_3"] = new(0.700, 106.7, 3.660, 0.069, 6.899, 2.544, 0.450, 2.0),
        ["N_2"] = new(0.685, 111.2, 3.660, 0.069, 6.899, 2.544, 0.0, 2.0),
        ["N_R"] = new(0.699, 120.0, 3.660, 0.069, 6.899, 2.544, 0.0, 2.0),
        ["N_1"] = new(0.656, 180.0, 3.660, 0.069, 6.899, 2.544, 0.0, 2.0),
        ["O_3"] = new(0.658, 104.51, 3.500, 0.060, 8.741, 2.300, 0.018, 2.0),
        ["O_2"] = new(0.634, 120.0, 3.500, 0.060, 8.741, 2.300, 0.0, 2.0),
        ["O_R"] = new(0.680, 110.0, 3.500, 0.060, 8.741, 2.300, 0.0, 2.0),
        ["F"] = new(0.668, 180.0, 3.364, 0.050, 10.874, 1.735, 0.0, 2.0),
        ["Cl"] = new(1.044, 180.0, 3.947, 0.227, 8.564, 2.348, 0.0, 1.25),
        ["Br"] = new(1.192, 180.0, 4.189, 0.251, 7.790, 2.519, 0.0, 0.7),
        ["I"] = new(1.382, 180.0, 4.500, 0.339, 6.822, 2.650, 0.0, 0.2),
        ["S_3+2"] = new(1.064, 92.1, 4.035, 0.274, 6.928, 2.703, 0.484, 1.25),
        ["S_3+4"] = new(1.049, 103.2, 4.035, 0.274, 6.928, 2.703, 0.484, 1.25),
        ["S_3+6"] = new(1.027, 109.47, 4.035, 0.274, 6.928, 2.703, 0.484, 1.25),
        ["S_2"] = new(0.854, 120.0, 4.035, 0.274, 6.928, 2.703, 0.0, 1.25),
        ["S_R"] = new(1.077, 92.2, 4.035, 0.274, 6.928, 2.703, 0.0, 1.25),
        ["P_3+3"] = new(1.101, 93.8, 4.147, 0.305, 5.463, 2.863, 2.4, 1.25),
        ["P_3+5"] = new(1.056, 109.47, 4.147, 0.305, 5.463, 2.863, 2.4, 1.25)
    };

    private readonly record struct BondParam(int Atom1, int Atom2, double R0, double Kr);
    private readonly record struct AngleParam(
        int Center,
        int N1,
        int N2,
        double Theta0Rad,
        double KTheta,
        int Order,
        double C0,
        double C1,
        double C2);
    private readonly record struct TorsionParam(
        int J,
        int K,
        int N1,
        int N2,
        double BarrierPerPair,
        int Order,
        double CosTerm);
    private readonly record struct InversionParam(
        int Center,
        int Axis,
        int N1,
        int N2,
        double KInvPerPermutation,
        double C0,
        double C1,
        double C2);
    private readonly record struct VdwParam(int Atom1, int Atom2, double Xij, double Dij, double Cutoff);

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

            var (r0, kr) = GetUffBondParametersFromParams(
                uffParams[i],
                uffParams[j],
                GetBondOrder(bond));
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

            double theta0 = uffParams[c].Theta0Deg * (Math.PI / 180.0);
            int order = IsSp1(atomTypes[c].TypeName) ? 1 : IsSp2(atomTypes[c].TypeName) ? 3 : 0;
            double c0 = 0.0;
            double c1 = 0.0;
            double c2 = 0.0;
            if (order == 0)
            {
                double sinTheta0 = Math.Sin(theta0);
                double cosTheta0 = Math.Cos(theta0);
                c2 = 1.0 / (4.0 * Math.Max(sinTheta0 * sinTheta0, 1e-8));
                c1 = -4.0 * c2 * cosTheta0;
                c0 = c2 * ((2.0 * cosTheta0 * cosTheta0) + 1.0);
            }

            for (int j = 0; j < neighbors.Count; j++)
            {
                for (int k = j + 1; k < neighbors.Count; k++)
                {
                    int n1 = neighbors[j];
                    int n2 = neighbors[k];
                    var bond1 = molecule.Bonds.First(bond => bond.Connects(c) && bond.Connects(n1));
                    var bond2 = molecule.Bonds.First(bond => bond.Connects(c) && bond.Connects(n2));
                    double kTheta = CalculateUffAngleForceConstant(
                        theta0,
                        GetBondOrder(bond1),
                        GetBondOrder(bond2),
                        uffParams[n1],
                        uffParams[c],
                        uffParams[n2]);
                    angleList.Add(new AngleParam(c, n1, n2, theta0, kTheta, order, c0, c1, c2));
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

            string typeJ = atomTypes[j].TypeName;
            string typeK = atomTypes[k].TypeName;
            bool jIsSp2 = IsSp2(typeJ);
            bool kIsSp2 = IsSp2(typeK);
            bool jIsSp3 = IsSp3(typeJ);
            bool kIsSp3 = IsSp3(typeK);
            if (!(jIsSp2 || jIsSp3) || !(kIsSp2 || kIsSp3))
            {
                continue;
            }

            double bondOrder = GetBondOrder(centralBond);
            double barrier;
            int torsionOrder;
            double cosTerm;
            if (jIsSp3 && kIsSp3)
            {
                barrier = Math.Sqrt(uffParams[j].Sp3TorsionBarrier * uffParams[k].Sp3TorsionBarrier);
                torsionOrder = 3;
                cosTerm = -1.0;
                if (bondOrder == 1.0 && IsGroup6(molecule.Atoms[j].Element.AtomicNumber) && IsGroup6(molecule.Atoms[k].Element.AtomicNumber))
                {
                    double v2 = molecule.Atoms[j].Element.AtomicNumber == 8 ? 2.0 : 6.8;
                    double v3 = molecule.Atoms[k].Element.AtomicNumber == 8 ? 2.0 : 6.8;
                    barrier = Math.Sqrt(v2 * v3);
                    torsionOrder = 2;
                }
            }
            else if (jIsSp2 && kIsSp2)
            {
                barrier = CalculateSp2TorsionBarrier(bondOrder, uffParams[j], uffParams[k]);
                torsionOrder = 2;
                cosTerm = 1.0;
            }
            else
            {
                barrier = 1.0;
                torsionOrder = 6;
                cosTerm = 1.0;
                int sp3Atom = jIsSp3 ? j : k;
                int sp2Atom = jIsSp2 ? j : k;
                if (bondOrder == 1.0 &&
                    IsGroup6(molecule.Atoms[sp3Atom].Element.AtomicNumber) &&
                    !IsGroup6(molecule.Atoms[sp2Atom].Element.AtomicNumber))
                {
                    barrier = CalculateSp2TorsionBarrier(bondOrder, uffParams[j], uffParams[k]);
                    torsionOrder = 2;
                    cosTerm = -1.0;
                }
            }

            foreach (var i in jNeighbors)
            {
                foreach (var l in kNeighbors)
                {
                    if (i != l && i < nAtoms && l < nAtoms)
                    {
                        double pairBarrier = barrier;
                        int pairOrder = torsionOrder;
                        double pairCosTerm = cosTerm;

                        // UFF's mixed sp2/sp3 rule depends on the two terminal atoms of
                        // each individual torsion, not on any terminal atom around the bond.
                        if ((jIsSp2 ^ kIsSp2) &&
                            bondOrder == 1.0 &&
                            !(IsGroup6(molecule.Atoms[jIsSp3 ? j : k].Element.AtomicNumber) &&
                              !IsGroup6(molecule.Atoms[jIsSp2 ? j : k].Element.AtomicNumber)) &&
                            (IsSp2(atomTypes[i].TypeName) || IsSp2(atomTypes[l].TypeName)))
                        {
                            pairBarrier = 2.0;
                            pairOrder = 3;
                            pairCosTerm = -1.0;
                        }

                        torsionList.Add(new TorsionParam(
                            j,
                            k,
                            i,
                            l,
                            pairBarrier / nPairs,
                            pairOrder,
                            pairCosTerm));
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
                int atomicNumber = molecule.Atoms[c].Element.AtomicNumber;
                bool isPlanarSecondRow = (atomicNumber is 6 or 7 or 8) && IsSp2(type.TypeName);
                bool isSupportedPyramidal = atomicNumber == 15;
                if (isPlanarSecondRow || isSupportedPyramidal)
                {
                    bool isCarbonylCarbon = atomicNumber == 6 && molecule.Bonds.Any(bond =>
                        bond.Connects(c) &&
                        bond.Type == BondType.Double &&
                        molecule.Atoms[bond.Atom1Index == c ? bond.Atom2Index : bond.Atom1Index].Element.Symbol == "O");
                    var coefficients = CalculateInversionCoefficients(atomicNumber, isCarbonylCarbon);
                    inversionList.Add(new InversionParam(c, neighbors[0], neighbors[1], neighbors[2], coefficients.K, coefficients.C0, coefficients.C1, coefficients.C2));
                    inversionList.Add(new InversionParam(c, neighbors[1], neighbors[0], neighbors[2], coefficients.K, coefficients.C0, coefficients.C1, coefficients.C2));
                    inversionList.Add(new InversionParam(c, neighbors[2], neighbors[0], neighbors[1], coefficients.K, coefficients.C0, coefficients.C1, coefficients.C2));
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
                    vdwList.Add(new VdwParam(i, j, x_ij, d_ij, 10.0 * x_ij));
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

    private static bool IsSp1(string atomType) => atomType.EndsWith("_1", StringComparison.Ordinal);

    private static bool IsSp2(string atomType) =>
        atomType.EndsWith("_2", StringComparison.Ordinal) || atomType.EndsWith("_R", StringComparison.Ordinal);

    private static bool IsSp3(string atomType) => atomType.Contains("_3", StringComparison.Ordinal);

    private static bool IsGroup6(int atomicNumber) => atomicNumber is 8 or 16 or 34 or 52 or 84;

    private static double GetBondOrder(Bond bond) => bond.Type switch
    {
        BondType.Triple => 3.0,
        BondType.Double => 2.0,
        BondType.Aromatic => 1.5,
        _ => 1.0
    };

    private static double CalculateUffAngleForceConstant(
        double theta0,
        double bondOrder12,
        double bondOrder23,
        UffAtomParams atom1,
        UffAtomParams center,
        UffAtomParams atom3)
    {
        double r12 = CalculateBondRestLength(atom1, center, bondOrder12);
        double r23 = CalculateBondRestLength(center, atom3, bondOrder23);
        double cosTheta0 = Math.Cos(theta0);
        double r13Squared = (r12 * r12) + (r23 * r23) - (2.0 * r12 * r23 * cosTheta0);
        double r13 = Math.Sqrt(Math.Max(r13Squared, 1e-16));
        double beta = (2.0 * 332.06) / (r12 * r23);
        double preFactor = beta * atom1.Z * atom3.Z / Math.Pow(r13, 5);
        double rTerm = r12 * r23;
        double inner = (3.0 * rTerm * (1.0 - (cosTheta0 * cosTheta0))) - (r13Squared * cosTheta0);
        return preFactor * rTerm * inner;
    }

    private static double CalculateSp2TorsionBarrier(double bondOrder, UffAtomParams atom2, UffAtomParams atom3) =>
        5.0 * Math.Sqrt(atom2.Sp2TorsionParameter * atom3.Sp2TorsionParameter) *
        (1.0 + (4.18 * Math.Log(bondOrder)));

    private static (double K, double C0, double C1, double C2) CalculateInversionCoefficients(
        int centerAtomicNumber,
        bool isCarbonylCarbon)
    {
        if (centerAtomicNumber is 6 or 7 or 8)
        {
            return ((isCarbonylCarbon ? 50.0 : 6.0) / 3.0, 1.0, -1.0, 0.0);
        }

        double w0 = centerAtomicNumber switch
        {
            15 => 84.4339 * Math.PI / 180.0,
            _ => throw new NotSupportedException($"UFF inversion parameters are unavailable for atomic number {centerAtomicNumber}.")
        };
        double c2 = 1.0;
        double c1 = -4.0 * Math.Cos(w0);
        double c0 = -((c1 * Math.Cos(w0)) + (c2 * Math.Cos(2.0 * w0)));
        double forceConstant = 22.0 / (c0 + c1 + c2) / 3.0;
        return (forceConstant, c0, c1, c2);
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
        var sourceMol = molecule3D.SourceMolecule ?? new Molecule(molecule3D.Name, molecule3D.Atoms.Select(a => a.Atom), Enumerable.Empty<Bond>());
        var applicability = AssessApplicability(sourceMol);
        ScientificApplicability.RequireWithinDomain(applicability, UffMethodInfo.Method);
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
            )
            {
                Applicability = applicability
            };
        }

        if (molecule3D.SourceMolecule != null && !molecule3D.SourceMolecule.HasBondedTopology)
        {
            throw new InvalidOperationException(
                $"Molecule '{molecule3D.Name}' has no bonded topology. Force field energy minimization requires a bonded molecular structure, not an empirical formula without connectivity.");
        }

        var topo = PrecomputeTopology(sourceMol, nAtoms);

        var currentPositions = new Vector3D[nAtoms];
        for (int i = 0; i < nAtoms; i++) currentPositions[i] = molecule3D.Atoms[i].Position;

        double currentEnergy = CalculateTotalPotentialEnergyFast(topo, currentPositions);
        double initialEnergy = currentEnergy;

        var reason = MinimizationTerminationReason.MaximumIterationsReached;
        int iterationsPerformed = 0;
        var gradients = ComputeFiniteDifferenceGradients(topo, currentPositions, 1e-5);
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

            // Numerical finite-difference noise or extreme short-range curvature can invalidate the quasi-Newton
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

            var candidateGradients = ComputeFiniteDifferenceGradients(topo, candidatePositions, 1e-5);
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
        )
        {
            Applicability = applicability
        };
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
        ScientificApplicability.RequireWithinDomain(AssessApplicability(sourceMol), UffMethodInfo.Method);
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
        ScientificApplicability.RequireWithinDomain(AssessApplicability(sourceMol), UffMethodInfo.Method);
        var topo = PrecomputeTopology(sourceMol, nAtoms);
        var positions = new Vector3D[nAtoms];
        for (int i = 0; i < nAtoms; i++) positions[i] = molecule3D.Atoms[i].Position;
        return CalculateEnergyComponentsFast(topo, positions);
    }

    /// <summary>
    /// Evaluates the Cartesian derivative of the exact implemented UFF potential using a
    /// symmetric two-sided finite difference. Coordinates and atom order are not mutated.
    /// </summary>
    public static ForceFieldGradientResult CalculateGradient(
        Molecule3D molecule3D,
        double finiteDifferenceStepAngstrom = 1e-5)
    {
        ArgumentNullException.ThrowIfNull(molecule3D);
        if (!double.IsFinite(finiteDifferenceStepAngstrom) || finiteDifferenceStepAngstrom <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finiteDifferenceStepAngstrom),
                finiteDifferenceStepAngstrom,
                "Finite-difference step must be a finite positive value in angstroms.");
        }

        int nAtoms = molecule3D.Atoms.Count;
        var sourceMol = molecule3D.SourceMolecule ?? new Molecule(
            molecule3D.Name,
            molecule3D.Atoms.Select(atom => atom.Atom),
            Enumerable.Empty<Bond>());
        var applicability = AssessApplicability(sourceMol);
        ScientificApplicability.RequireWithinDomain(applicability, UffMethodInfo.Method);
        var topology = PrecomputeTopology(sourceMol, nAtoms);
        var positions = molecule3D.Atoms.Select(atom => atom.Position).ToArray();
        var gradient = ComputeFiniteDifferenceGradients(
            topology,
            positions,
            finiteDifferenceStepAngstrom);

        return new ForceFieldGradientResult(
            gradient,
            finiteDifferenceStepAngstrom,
            MaxAbsComponent(gradient),
            UffMethodInfo)
        {
            Applicability = applicability
        };
    }

    /// <summary>Evaluates whether a molecular graph can be parameterized by the declared UFF subset.</summary>
    public static ScientificApplicabilityAssessment AssessApplicability(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);
        var supportedElements = new HashSet<string>(
            ["H", "C", "N", "O", "P", "S", "F", "Cl", "Br", "I"],
            StringComparer.Ordinal);

        var structural = ScientificApplicability.AssessMolecule(molecule, supportedElements);
        if (!structural.IsWithinDomain) return structural;

        var reasons = new List<string>(structural.Reasons);
        for (int atomIndex = 0; atomIndex < molecule.Atoms.Count; atomIndex++)
        {
            try
            {
                _ = GetUffParams(GetUffAtomType(molecule, atomIndex).TypeName);
            }
            catch (NotSupportedException exception)
            {
                reasons.Add($"Atom {atomIndex}: {exception.Message}");
            }
        }

        return reasons.Count == structural.Reasons.Count
            ? structural
            : new ScientificApplicabilityAssessment(ApplicabilityStatus.OutOfDomain, reasons);
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
            double angleTerm;
            if (angle.Order == 0)
            {
                angleTerm = angle.C0 +
                    (angle.C1 * Math.Cos(angleRad)) +
                    (angle.C2 * Math.Cos(2.0 * angleRad));
            }
            else
            {
                // The published UFF linear term is 1 + cos(theta); higher-order
                // periodic terms use 1 - cos(n*theta), divided by n^2.
                angleTerm = angle.Order == 1
                    ? 1.0 + Math.Cos(angleRad)
                    : (1.0 - Math.Cos(angle.Order * angleRad)) /
                        (angle.Order * angle.Order);
                if (angle.Order < 5 && angleRad < Math.PI / 6.0)
                {
                    angleTerm += Math.Exp(-20.0 * (angleRad - angle.Theta0Rad + 0.25)) / angle.KTheta;
                }
            }
            eAngle += angle.KTheta * angleTerm;
        }

        // 3. UFF Dihedral Torsional Strain
        var torsions = topo.Torsions;
        for (int t = 0; t < torsions.Length; t++)
        {
            ref readonly var tor = ref torsions[t];
            double phiRad = CalculateDihedralAngleRad(positions[tor.N1], positions[tor.J], positions[tor.K], positions[tor.N2]);
            eTorsion += 0.5 * tor.BarrierPerPair *
                (1.0 - (tor.CosTerm * Math.Cos(tor.Order * phiRad)));
        }

        // 4. UFF nonbonded 12-6 Lennard-Jones potential with geometric-mean parameters.
        var vdws = topo.Vdws;
        for (int v = 0; v < vdws.Length; v++)
        {
            ref readonly var vdw = ref vdws[v];
            double dist = Distance(positions[vdw.Atom1], positions[vdw.Atom2]);
            if (dist <= 0.0 || dist > vdw.Cutoff)
            {
                continue;
            }
            double ratio = vdw.Xij / dist;
            double term6 = Math.Pow(ratio, 6);
            double term12 = term6 * term6;
            eVdw += vdw.Dij * (term12 - (2.0 * term6));
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
                double cosY = (vAxis.X * normPlane.X + vAxis.Y * normPlane.Y + vAxis.Z * normPlane.Z) / lenAxis;
                cosY = Math.Clamp(cosY, -1.0, 1.0);
                double sinY = Math.Sqrt(Math.Max(0.0, 1.0 - (cosY * cosY)));
                double cos2W = (2.0 * sinY * sinY) - 1.0;
                eInversion += item.KInvPerPermutation *
                    (item.C0 + (item.C1 * sinY) + (item.C2 * cos2W));
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
        bool isAmideResonanceAtom = IsAmideResonanceAtom(molecule, atomIndex);

        switch (symbol)
        {
            case "H":
                return ("H", 180.0, 0.354);

            case "C":
                if (isAmideResonanceAtom) return ("C_R", 120.0, 0.729);
                if (hasAromatic) return ("C_R", 120.0, 0.729);
                if (hasTriple || doubleCount >= 2) return ("C_1", 180.0, 0.706);
                if (hasDouble) return ("C_2", 120.0, 0.732);
                return ("C_3", 109.4712, 0.757);

            case "N":
                if (hasAromatic) return ("N_R", 120.0, 0.699);
                if (hasTriple) return ("N_1", 180.0, 0.656);
                if (hasDouble) return ("N_2", 111.2, 0.685);

                // Chemy's graph model does not carry a hybridization flag. Mirror
                // the sanitized-molecule typing used by UFF for conjugated amide N.
                bool isResonantPlanar = bonds.Any(bond =>
                {
                    int neighbor = bond.Atom1Index == atomIndex ? bond.Atom2Index : bond.Atom1Index;
                    if (molecule.Atoms[neighbor].Element.Symbol is not ("C" or "S" or "P"))
                    {
                        return false;
                    }

                    return molecule.Bonds.Any(candidate =>
                        candidate.Connects(neighbor) &&
                        candidate.Type == BondType.Double &&
                        molecule.Atoms[candidate.Atom1Index == neighbor
                            ? candidate.Atom2Index
                            : candidate.Atom1Index].Element.Symbol is "O" or "S" or "N");
                });
                if (isResonantPlanar) return ("N_R", 120.0, 0.699);

                if (coord >= 4)
                {
                    return ("N_3", 109.4712, 0.700); // sp3 tetrahedral ammonium
                }

                return ("N_3", 106.70, 0.700); // sp3 pyramidal amine / ammonia

            case "O":
                if (isAmideResonanceAtom) return ("O_R", 110.0, 0.680);
                if (hasAromatic) return ("O_R", 110.0, 0.680);
                if (hasDouble) return ("O_2", 120.0, 0.634);
                return ("O_3", 104.51, 0.658);

            case "P":
                if (coord >= 4) return ("P_3+5", 109.47, 1.056);
                return ("P_3+3", 93.8, 1.101); // sp3 pyramidal phosphine

            case "S":
                if (hasAromatic) return ("S_R", 92.2, 1.077);
                if (hasDouble && coord <= 2) return ("S_2", 120.0, 0.854);
                if (coord == 2 && bonds.All(b => molecule.Atoms[b.Atom1Index == atomIndex ? b.Atom2Index : b.Atom1Index].Element.Symbol == "H"))
                {
                    return ("S_3+2", 92.1, 1.064); // H2S bent
                }
                if (coord >= 6) return ("S_3+6", 109.47, 1.027);
                if (coord >= 3 || hasDouble) return ("S_3+4", 103.2, 1.049);
                return ("S_3+2", 92.1, 1.064);

            case "F":
                return ("F", 180.0, 0.668);

            case "Cl":
                return ("Cl", 180.0, 1.044);

            case "Br":
                return ("Br", 180.0, 1.192);

            case "I":
                return ("I", 180.0, 1.382);

            default:
                throw new NotSupportedException(
                    $"UFF atom typing is not implemented for element '{symbol}'. Supported elements are H, C, N, O, P, S, F, Cl, Br, and I.");
        }
    }

    private static bool IsAmideResonanceAtom(Molecule molecule, int atomIndex)
    {
        string symbol = molecule.Atoms[atomIndex].Element.Symbol;
        if (symbol == "N")
        {
            return molecule.Bonds.Where(bond => bond.Connects(atomIndex)).Any(bond =>
            {
                int carbon = bond.Atom1Index == atomIndex ? bond.Atom2Index : bond.Atom1Index;
                return bond.Type == BondType.Single &&
                    molecule.Atoms[carbon].Element.Symbol == "C" &&
                    molecule.Bonds.Any(candidate =>
                        candidate.Connects(carbon) &&
                        candidate.Type == BondType.Double &&
                        molecule.Atoms[candidate.Atom1Index == carbon
                            ? candidate.Atom2Index
                            : candidate.Atom1Index].Element.Symbol == "O");
            });
        }

        if (symbol == "C")
        {
            bool hasCarbonylOxygen = molecule.Bonds.Any(bond =>
                bond.Connects(atomIndex) &&
                bond.Type == BondType.Double &&
                molecule.Atoms[bond.Atom1Index == atomIndex ? bond.Atom2Index : bond.Atom1Index].Element.Symbol == "O");
            bool hasAmideNitrogen = molecule.Bonds.Any(bond =>
                bond.Connects(atomIndex) &&
                bond.Type == BondType.Single &&
                molecule.Atoms[bond.Atom1Index == atomIndex ? bond.Atom2Index : bond.Atom1Index].Element.Symbol == "N");
            return hasCarbonylOxygen && hasAmideNitrogen;
        }

        if (symbol == "O")
        {
            return molecule.Bonds.Where(bond => bond.Connects(atomIndex) && bond.Type == BondType.Double).Any(bond =>
            {
                int carbon = bond.Atom1Index == atomIndex ? bond.Atom2Index : bond.Atom1Index;
                return molecule.Atoms[carbon].Element.Symbol == "C" &&
                    molecule.Bonds.Any(candidate =>
                        candidate.Connects(carbon) &&
                        candidate.Type == BondType.Single &&
                        molecule.Atoms[candidate.Atom1Index == carbon
                            ? candidate.Atom2Index
                            : candidate.Atom1Index].Element.Symbol == "N");
            });
        }

        return false;
    }

    /// <summary>
    /// Computes central finite-difference gradients over the exact implemented potential energy function.
    /// </summary>
    private static Vector3D[] ComputeFiniteDifferenceGradients(
        TopologyParams topo,
        Vector3D[] positions,
        double h)
    {
        int nAtoms = positions.Length;
        var grads = new Vector3D[nAtoms];

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

    private static (double R0, double Kr) GetUffBondParametersFromParams(
        UffAtomParams p1,
        UffAtomParams p2,
        double bondOrder)
    {
        double r0 = CalculateBondRestLength(p1, p2, bondOrder);
        double kr = 664.12 * (p1.Z * p2.Z) / (r0 * r0 * r0);
        return (r0, kr);
    }

    private static double CalculateBondRestLength(UffAtomParams p1, UffAtomParams p2, double bondOrder)
    {
        // Rappé eq 2: r_BO = -0.1332 * (r1 + r2) * ln(BO)
        double r_bo = -0.1332 * (p1.R0 + p2.R0) * Math.Log(bondOrder);

        // Rappé eq 3: Electronegativity correction
        double chiDiff = Math.Sqrt(p1.Chi) - Math.Sqrt(p2.Chi);
        double r_en = (p1.R0 * p2.R0 * chiDiff * chiDiff) / (p1.Chi * p1.R0 + p2.Chi * p2.R0);

        return p1.R0 + p2.R0 + r_bo - r_en;
    }

    private static UffAtomParams GetUffParams(string typeName)
    {
        if (UffParams.TryGetValue(typeName, out var p)) return p;
        throw new NotSupportedException($"UFF parameters are unavailable for atom type '{typeName}'.");
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
