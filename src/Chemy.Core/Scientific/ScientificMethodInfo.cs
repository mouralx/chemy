namespace Chemy.Core.Scientific;

/// <summary>Strength of evidence behind a reported scientific result.</summary>
public enum EvidenceLevel
{
    ExactEquation,
    NumericalApproximation,
    EmpiricalModel,
    Heuristic
}

/// <summary>Machine-readable provenance and applicability information for a scientific result.</summary>
public sealed record ScientificMethodInfo(
    string Method,
    string Version,
    EvidenceLevel EvidenceLevel,
    string ApplicabilityDomain,
    IReadOnlyList<string> Warnings);
