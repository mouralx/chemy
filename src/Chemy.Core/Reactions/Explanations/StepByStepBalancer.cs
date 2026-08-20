using System.Text;

namespace Chemy.Core.Reactions.Explanations;

public static class StepByStepBalancer
{
    public static BalancedReactionWithSteps BalanceWithSteps(Reaction reaction)
    {
        ArgumentNullException.ThrowIfNull(reaction);

        var steps = new List<ExplanationStep>();
        var balanced = reaction.Balance();

        var allComponents = reaction.Reactants.Concat(reaction.Products).ToList();
        var allElements = allComponents
            .SelectMany(c => c.Molecule.Atoms.Select(a => a.Element))
            .Distinct()
            .ToList();

        var reactantCounts = GetElementCounts(reaction.Reactants);
        var productCounts = GetElementCounts(reaction.Products);

        var step1Builder = new StringBuilder();
        foreach (var elem in allElements)
        {
            int rCount = reactantCounts.GetValueOrDefault(elem, 0);
            int pCount = productCounts.GetValueOrDefault(elem, 0);
            string status = rCount == pCount ? "Balanced" : "Unbalanced";
            step1Builder.AppendLine($"Element {elem.Symbol} ({elem.Name}): Reactants = {rCount}, Products = {pCount} [{status}]");
        }

        steps.Add(new ExplanationStep(
            1,
            "Initial Atom Count Audit",
            "Count the number of atoms of each element on both sides of the reaction.",
            step1Builder.ToString().TrimEnd()
        ));

        var step2Builder = new StringBuilder();
        char varName = 'a';
        var variableNames = allComponents.Select((_, i) => (char)(varName + i)).ToList();
        for (int i = 0; i < reaction.Reactants.Count; i++)
        {
            step2Builder.AppendLine($"Reactant {i + 1} ({reaction.Reactants[i].Molecule.Name}): coefficient {variableNames[i]}");
        }
        for (int i = 0; i < reaction.Products.Count; i++)
        {
            int idx = reaction.Reactants.Count + i;
            step2Builder.AppendLine($"Product {i + 1} ({reaction.Products[i].Molecule.Name}): coefficient {variableNames[idx]}");
        }
        step2Builder.AppendLine();
        step2Builder.AppendLine("Conservation Equations per Element:");
        foreach (var elem in allElements)
        {
            var rTerms = new List<string>();
            for (int i = 0; i < reaction.Reactants.Count; i++)
            {
                int c = reaction.Reactants[i].Molecule.Atoms.Count(a => a.Element == elem);
                if (c > 0) rTerms.Add(c == 1 ? $"{variableNames[i]}" : $"{c}{variableNames[i]}");
            }

            var pTerms = new List<string>();
            for (int i = 0; i < reaction.Products.Count; i++)
            {
                int idx = reaction.Reactants.Count + i;
                int c = reaction.Products[i].Molecule.Atoms.Count(a => a.Element == elem);
                if (c > 0) pTerms.Add(c == 1 ? $"{variableNames[idx]}" : $"{c}{variableNames[idx]}");
            }

            string rEq = rTerms.Count > 0 ? string.Join(" + ", rTerms) : "0";
            string pEq = pTerms.Count > 0 ? string.Join(" + ", pTerms) : "0";
            step2Builder.AppendLine($"{elem.Symbol}: {rEq} = {pEq}");
        }

        steps.Add(new ExplanationStep(
            2,
            "Setting Up Conservation Equations",
            "Assign stoichiometric variables (a, b, c...) to each compound and set up conservation of mass equations for each element.",
            step2Builder.ToString().TrimEnd()
        ));

        int rows = allElements.Count;
        int cols = allComponents.Count;
        var step3Builder = new StringBuilder();
        step3Builder.AppendLine($"System matrix size: {rows} elements x {cols} compounds");
        for (int r = 0; r < rows; r++)
        {
            var rowVals = new List<string>();
            var elem = allElements[r];
            for (int c = 0; c < cols; c++)
            {
                int atomCount = allComponents[c].Molecule.Atoms.Count(a => a.Element == elem);
                int val = c < reaction.Reactants.Count ? atomCount : -atomCount;
                rowVals.Add(val.ToString().PadLeft(3));
            }
            step3Builder.AppendLine($"[{string.Join(", ", rowVals)}] ({elem.Symbol})");
        }

        steps.Add(new ExplanationStep(
            3,
            "Matrix Representation & Gaussian Elimination",
            "Formulate the system matrix M where reactants are positive and products are negative, then solve for the integer nullspace vector.",
            step3Builder.ToString().TrimEnd()
        ));

        var step4Builder = new StringBuilder();
        for (int i = 0; i < balanced.Reactants.Count; i++)
        {
            step4Builder.AppendLine($"{variableNames[i]} = {balanced.Reactants[i].Coefficient} ({balanced.Reactants[i].Molecule.Name})");
        }
        for (int i = 0; i < balanced.Products.Count; i++)
        {
            int idx = balanced.Reactants.Count + i;
            step4Builder.AppendLine($"{variableNames[idx]} = {balanced.Products[i].Coefficient} ({balanced.Products[i].Molecule.Name})");
        }

        steps.Add(new ExplanationStep(
            4,
            "Clearing Fractions & Integer Scaling",
            "Find the least common multiple (LCM) of denominators to scale coefficients to the lowest positive integer values.",
            step4Builder.ToString().TrimEnd()
        ));

        var finalRCounts = GetElementCounts(balanced.Reactants);
        var finalPCounts = GetElementCounts(balanced.Products);
        var step5Builder = new StringBuilder();
        step5Builder.AppendLine($"Balanced Equation: {balanced}");
        step5Builder.AppendLine();
        foreach (var elem in allElements)
        {
            step5Builder.AppendLine($"{elem.Symbol}: {finalRCounts[elem]} on reactants = {finalPCounts[elem]} on products");
        }

        steps.Add(new ExplanationStep(
            5,
            "Final Balance Verification",
            "Verify that atom counts match perfectly for every element.",
            step5Builder.ToString().TrimEnd()
        ));

        return new BalancedReactionWithSteps(balanced, steps);
    }

    private static Dictionary<Element, int> GetElementCounts(IEnumerable<ReactionComponent> components)
    {
        var counts = new Dictionary<Element, int>();
        foreach (var comp in components)
        {
            foreach (var atom in comp.Molecule.Atoms)
            {
                counts[atom.Element] = counts.GetValueOrDefault(atom.Element, 0) + comp.Coefficient;
            }
        }
        return counts;
    }
}
