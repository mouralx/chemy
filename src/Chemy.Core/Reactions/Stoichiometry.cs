namespace Chemy.Core.Reactions;

public record ProductYieldResult(
    Molecule Reactant,
    double ReactantMassGrams,
    double ReactantMoles,
    Molecule Product,
    double ProductMoles,
    double ProductMassGrams
);

public record LimitingReactantResult(
    ReactionComponent LimitingReactant,
    IReadOnlyDictionary<string, double> ProductYieldsGrams
);

public static class Stoichiometry
{
    public static ProductYieldResult CalculateProductYield(Reaction reaction, string reactantFormula, double reactantMassGrams, string productFormula)
    {
        ArgumentNullException.ThrowIfNull(reaction);
        ArgumentException.ThrowIfNullOrWhiteSpace(reactantFormula);
        ArgumentException.ThrowIfNullOrWhiteSpace(productFormula);
        ArgumentOutOfRangeException.ThrowIfNegative(reactantMassGrams);

        var balanced = reaction.IsBalanced ? reaction : reaction.Balance();

        var reactantComp = balanced.Reactants.FirstOrDefault(r => r.Molecule.ChemicalFormula.Equals(reactantFormula, StringComparison.OrdinalIgnoreCase) || r.Molecule.Name.Equals(reactantFormula, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Reactant '{reactantFormula}' not found in reaction.");

        var productComp = balanced.Products.FirstOrDefault(p => p.Molecule.ChemicalFormula.Equals(productFormula, StringComparison.OrdinalIgnoreCase) || p.Molecule.Name.Equals(productFormula, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Product '{productFormula}' not found in reaction.");

        double reactantMoles = reactantMassGrams / reactantComp.Molecule.MolecularWeight;
        double moleRatio = (double)productComp.Coefficient / reactantComp.Coefficient;
        double productMoles = reactantMoles * moleRatio;
        double productMassGrams = productMoles * productComp.Molecule.MolecularWeight;

        return new ProductYieldResult(
            reactantComp.Molecule,
            reactantMassGrams,
            reactantMoles,
            productComp.Molecule,
            productMoles,
            productMassGrams
        );
    }

    public static LimitingReactantResult CalculateLimitingReactant(Reaction reaction, IReadOnlyDictionary<string, double> reactantMassesGrams)
    {
        ArgumentNullException.ThrowIfNull(reaction);
        ArgumentNullException.ThrowIfNull(reactantMassesGrams);

        var balanced = reaction.IsBalanced ? reaction : reaction.Balance();

        ReactionComponent? limiting = null;
        double minReactionRuns = double.MaxValue;

        foreach (var reactantComp in balanced.Reactants)
        {
            string key = reactantComp.Molecule.ChemicalFormula;
            if (!reactantMassesGrams.TryGetValue(key, out double massGrams))
            {
                key = reactantComp.Molecule.Name;
                if (!reactantMassesGrams.TryGetValue(key, out massGrams))
                {
                    throw new ArgumentException($"Mass for reactant '{reactantComp.Molecule.ChemicalFormula}' was not provided.");
                }
            }

            double moles = massGrams / reactantComp.Molecule.MolecularWeight;
            double reactionRuns = moles / reactantComp.Coefficient;

            if (reactionRuns < minReactionRuns)
            {
                minReactionRuns = reactionRuns;
                limiting = reactantComp;
            }
        }

        if (limiting == null) throw new InvalidOperationException("No reactants found.");

        var yields = new Dictionary<string, double>();
        foreach (var productComp in balanced.Products)
        {
            double productMoles = minReactionRuns * productComp.Coefficient;
            double productMassGrams = productMoles * productComp.Molecule.MolecularWeight;
            yields[productComp.Molecule.ChemicalFormula] = productMassGrams;
        }

        return new LimitingReactantResult(limiting, yields);
    }
}
