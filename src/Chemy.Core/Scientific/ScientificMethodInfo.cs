namespace Chemy.Core.Scientific;

/// <summary>
/// Strength of mathematical or empirical evidence supporting a scientific computation.
/// </summary>
public enum EvidenceLevel
{
    /// <summary>Exact closed-form analytical or rational algebraic equation (e.g. Nernst, Halley cubic, Jacobi eigensolver, Stoichiometric nullspace).</summary>
    ExactEquation,

    /// <summary>Numerically converged differential or variational algorithm (e.g. RK4, L-BFGS, finite-difference gradient).</summary>
    NumericalApproximation,

    /// <summary>Peer-reviewed published empirical or QSAR calibration model (e.g. Ertl 2000 TPSA, Wildman-Crippen 1999 LogP, Bickerton 2012 QED, UFF 1992).</summary>
    EmpiricalModel,

    /// <summary>Educational qualitative rule or structural filter (e.g. Lipinski Rule of 5, Veber filters, functional-group detection).</summary>
    Heuristic
}

/// <summary>
/// Machine-readable provenance and applicability metadata for scientific calculations.
/// </summary>
/// <param name="Method">Formal name of the algorithm or model (with literature reference where applicable).</param>
/// <param name="Version">Method or parameterization version identifier.</param>
/// <param name="EvidenceLevel">Level of mathematical or empirical rigor backing the result.</param>
/// <param name="ApplicabilityDomain">Valid chemical scope, boundary conditions, and assumptions.</param>
/// <param name="Warnings">Active notices, boundary caveats, or known failure modes for this specific calculation.</param>
public sealed record ScientificMethodInfo(
    string Method,
    string Version,
    EvidenceLevel EvidenceLevel,
    string ApplicabilityDomain,
    IReadOnlyList<string> Warnings
);
