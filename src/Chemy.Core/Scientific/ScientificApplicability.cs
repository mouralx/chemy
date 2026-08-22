namespace Chemy.Core.Scientific;

/// <summary>Shared, deterministic applicability checks for molecular prediction models.</summary>
public static class ScientificApplicability
{
    /// <summary>
    /// Evaluates a bonded molecular graph against an explicit element and formal-charge domain.
    /// </summary>
    public static ScientificApplicabilityAssessment AssessMolecule(
        Molecule molecule,
        IReadOnlySet<string> supportedElements,
        int maximumAbsoluteFormalCharge = 1)
    {
        ArgumentNullException.ThrowIfNull(molecule);
        ArgumentNullException.ThrowIfNull(supportedElements);

        var reasons = new List<string>();
        if (!molecule.HasBondedTopology)
        {
            reasons.Add("A bonded molecular graph is required; empirical formula input has no unique topology.");
        }

        string[] unsupported = molecule.Atoms
            .Select(atom => atom.Element.Symbol)
            .Where(symbol => !supportedElements.Contains(symbol))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unsupported.Length > 0)
        {
            reasons.Add($"Unsupported elements: {string.Join(", ", unsupported)}.");
        }

        int formalCharge = molecule.Atoms.Sum(atom => atom.NetCharge);
        if (Math.Abs(formalCharge) > maximumAbsoluteFormalCharge)
        {
            reasons.Add($"Net formal charge {formalCharge:+#;-#;0} exceeds the validated magnitude of {maximumAbsoluteFormalCharge}.");
        }

        if (reasons.Count > 0)
        {
            return new ScientificApplicabilityAssessment(ApplicabilityStatus.OutOfDomain, reasons);
        }

        if (formalCharge != 0)
        {
            return new ScientificApplicabilityAssessment(
                ApplicabilityStatus.Boundary,
                [$"Singly charged input ({formalCharge:+#;-#;0}) is accepted but has less validation coverage than neutral molecules."]);
        }

        return new ScientificApplicabilityAssessment(
            ApplicabilityStatus.InDomain,
            ["Bonded topology, supported elements, and formal charge are inside the declared domain."]);
    }

    /// <summary>Throws a stable scientific-domain exception when an assessment is out of domain.</summary>
    public static void RequireWithinDomain(
        ScientificApplicabilityAssessment assessment,
        string methodName)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        if (!assessment.IsWithinDomain)
        {
            throw new NotSupportedException(
                $"{methodName} rejected an out-of-domain input: {string.Join(" ", assessment.Reasons)}");
        }
    }
}
