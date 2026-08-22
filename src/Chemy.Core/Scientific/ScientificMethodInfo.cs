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
)
{
    /// <summary>Executable benchmark evidence attached to this exact implementation version, when available.</summary>
    public ScientificValidationEvidence? ValidationEvidence { get; init; }

    /// <summary>Primary literature, standard, or reference-implementation identifiers.</summary>
    public IReadOnlyList<string> ReferenceUris { get; init; } = Array.Empty<string>();
}

/// <summary>Machine-readable applicability decision for one concrete scientific input.</summary>
public enum ApplicabilityStatus
{
    InDomain,
    Boundary,
    OutOfDomain
}

/// <summary>
/// Result of evaluating a concrete input against a method's declared scientific domain.
/// Out-of-domain inputs must fail closed before a numerical prediction is returned.
/// </summary>
public sealed record ScientificApplicabilityAssessment(
    ApplicabilityStatus Status,
    IReadOnlyList<string> Reasons)
{
    public bool IsWithinDomain => Status is ApplicabilityStatus.InDomain or ApplicabilityStatus.Boundary;
}

/// <summary>One named error or agreement statistic from a frozen validation artifact.</summary>
public sealed record ScientificValidationMetric(
    string Name,
    double Value,
    string Unit);

/// <summary>
/// Reproducible evidence for an implementation version. Independence and prospectivity are
/// explicit so regression evidence cannot be presented as external certification.
/// </summary>
public sealed record ScientificValidationEvidence(
    string DatasetId,
    string DatasetVersion,
    int SampleSize,
    IReadOnlyList<ScientificValidationMetric> Metrics,
    string ArtifactPath,
    string ArtifactSha256,
    bool IndependentlyCurated,
    bool ProspectivelyFrozen);

/// <summary>
/// Calibrated empirical error envelope for a returned estimate. This is not a confidence
/// interval unless <see cref="Interpretation"/> explicitly states that it is one.
/// </summary>
public sealed record ScientificUncertainty(
    double AbsoluteErrorEnvelope,
    string Unit,
    double CoverageFraction,
    string Interpretation,
    string DatasetId);

/// <summary>Per-run numerical quality indicators for iterative solvers and integrators.</summary>
public sealed record ScientificNumericalDiagnostics(
    bool Converged,
    double StepSize,
    double MaximumResidual,
    double MaximumConservationError,
    string ResidualUnit,
    string Notes);
