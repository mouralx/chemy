using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Chemy.Core.Parsing;
using Chemy.Core.Reactions;
using Chemy.Core.Reactions.Explanations;
using Chemy.Core.Rendering;
using Chemy.Core.Thermodynamics;

namespace Chemy.Core;

/// <summary>
/// Immutable chemical reaction entity encapsulating reactants, products, stoichiometric balancing,
/// educational step explanations, Hess's Law thermodynamics, and vector SVG diagram rendering.
/// </summary>
public record Reaction
{
    /// <summary>Immutable list of reactant components.</summary>
    public ImmutableList<ReactionComponent> Reactants { get; init; }

    /// <summary>Immutable list of product components.</summary>
    public ImmutableList<ReactionComponent> Products { get; init; }

    /// <summary>
    /// Evaluates whether the Law of Conservation of Mass is satisfied
    /// (total atom count for each element is identical on reactant and product sides).
    /// </summary>
    public bool IsBalanced
    {
        get
        {
            var reactantCounts = GetElementCounts(Reactants);
            var productCounts = GetElementCounts(Products);

            if (reactantCounts.Count != productCounts.Count) return false;

            foreach (var (element, count) in reactantCounts)
            {
                if (!productCounts.TryGetValue(element, out int prodCount) || count != prodCount)
                {
                    return false;
                }
            }

            int reactantCharge = Reactants.Sum(r => r.Coefficient * r.Molecule.NetCharge);
            int productCharge = Products.Sum(p => p.Coefficient * p.Molecule.NetCharge);
            if (reactantCharge != productCharge) return false;

            return true;
        }
    }

    /// <summary>
    /// Constructs a Reaction instance with validated reactant and product lists.
    /// </summary>
    /// <param name="reactants">Collection of reactant components.</param>
    /// <param name="products">Collection of product components.</param>
    public Reaction(IEnumerable<ReactionComponent> reactants, IEnumerable<ReactionComponent> products)
    {
        ArgumentNullException.ThrowIfNull(reactants);
        ArgumentNullException.ThrowIfNull(products);

        Reactants = reactants.ToImmutableList();
        Products = products.ToImmutableList();

        if (Reactants.Count == 0 || Products.Count == 0)
        {
            throw new ArgumentException("Reaction must have at least one reactant and one product.");
        }
    }

    /// <summary>
    /// Balances the chemical equation using exact rational Gaussian elimination nullspace algebra.
    /// Computes the minimal integer stoichiometric coefficients satisfying M * x = 0 with mass and charge conservation.
    /// </summary>
    /// <returns>New balanced Reaction instance.</returns>
    public Reaction Balance()
    {
        if (IsBalanced) return this;

        var allComponents = Reactants.Concat(Products).ToList();
        var allElements = allComponents
            .SelectMany(c => c.Molecule.Atoms.Select(a => a.Element))
            .Distinct()
            .ToList();

        bool hasCharges = allComponents.Any(c => c.Molecule.NetCharge != 0);
        int rows = allElements.Count + (hasCharges ? 1 : 0);
        int cols = allComponents.Count;
        int[,] matrix = new int[rows, cols];

        // Formulate stoichiometric conservation matrix M
        for (int r = 0; r < allElements.Count; r++)
        {
            var elem = allElements[r];
            for (int c = 0; c < cols; c++)
            {
                int atomCount = allComponents[c].Molecule.Atoms.Count(a => a.Element == elem);
                matrix[r, c] = c < Reactants.Count ? atomCount : -atomCount;
            }
        }

        // Add net charge conservation row for ionic redox reactions
        if (hasCharges)
        {
            int chargeRow = allElements.Count;
            for (int c = 0; c < cols; c++)
            {
                int charge = allComponents[c].Molecule.NetCharge;
                matrix[chargeRow, c] = c < Reactants.Count ? charge : -charge;
            }
        }

        var basis = MatrixSolver.SolveNullspaceBasis(matrix);
        if (basis.Count == 0)
        {
            throw new InvalidOperationException("Could not balance reaction automatically: no valid non-trivial stoichiometric solution exists.");
        }

        if (basis.Count > 1)
        {
            throw new InvalidOperationException($"The chemical reaction is underdetermined with {basis.Count} independent fundamental reaction pathways (nullspace dimension = {basis.Count}). Call BalanceIndependentPathways() to obtain each independent stoichiometric reaction.");
        }

        var coefficients = basis[0];
        var balancedReactants = Reactants.Select((c, i) => new ReactionComponent(c.Molecule, coefficients[i]));
        var balancedProducts = Products.Select((c, i) => new ReactionComponent(c.Molecule, coefficients[Reactants.Count + i]));

        return new Reaction(balancedReactants, balancedProducts);
    }

    /// <summary>
    /// Decomposes an underdetermined chemical system into its complete basis of independent, balanced fundamental reactions.
    /// </summary>
    public IReadOnlyList<Reaction> BalanceIndependentPathways()
    {
        var allComponents = Reactants.Concat(Products).ToList();
        var allElements = allComponents
            .SelectMany(c => c.Molecule.Atoms.Select(a => a.Element))
            .Distinct()
            .ToList();

        bool hasCharges = allComponents.Any(c => c.Molecule.NetCharge != 0);
        int rows = allElements.Count + (hasCharges ? 1 : 0);
        int cols = allComponents.Count;
        int[,] matrix = new int[rows, cols];

        for (int r = 0; r < allElements.Count; r++)
        {
            var elem = allElements[r];
            for (int c = 0; c < cols; c++)
            {
                int atomCount = allComponents[c].Molecule.Atoms.Count(a => a.Element == elem);
                matrix[r, c] = c < Reactants.Count ? atomCount : -atomCount;
            }
        }

        if (hasCharges)
        {
            int chargeRow = allElements.Count;
            for (int c = 0; c < cols; c++)
            {
                int charge = allComponents[c].Molecule.NetCharge;
                matrix[chargeRow, c] = c < Reactants.Count ? charge : -charge;
            }
        }

        var basis = MatrixSolver.SolveNullspaceBasis(matrix);
        var pathways = new List<Reaction>();

        foreach (var rawVec in basis)
        {
            var vec = (int[])rawVec.Clone();

            // Count effective reactant vs product direction
            int reactantNet = 0;
            for (int i = 0; i < Reactants.Count; i++) reactantNet += vec[i];
            for (int i = 0; i < Products.Count; i++) reactantNet -= vec[Reactants.Count + i];

            if (reactantNet < 0)
            {
                for (int i = 0; i < vec.Length; i++) vec[i] = -vec[i];
            }

            var balancedReactants = new List<ReactionComponent>();
            var balancedProducts = new List<ReactionComponent>();

            for (int i = 0; i < allComponents.Count; i++)
            {
                var mol = allComponents[i].Molecule;
                int signCoeff = i < Reactants.Count ? vec[i] : -vec[i];

                if (signCoeff > 0)
                {
                    balancedReactants.Add(new ReactionComponent(mol, signCoeff));
                }
                else if (signCoeff < 0)
                {
                    balancedProducts.Add(new ReactionComponent(mol, -signCoeff));
                }
            }

            if (balancedReactants.Count > 0 && balancedProducts.Count > 0)
            {
                // Verify stoichiometric element and charge conservation invariant
                bool conserved = true;
                foreach (var elem in allElements)
                {
                    int inCount = balancedReactants.Sum(r => r.Molecule.Atoms.Count(a => a.Element == elem) * r.Coefficient);
                    int outCount = balancedProducts.Sum(p => p.Molecule.Atoms.Count(a => a.Element == elem) * p.Coefficient);
                    if (inCount != outCount) { conserved = false; break; }
                }

                if (hasCharges)
                {
                    int inCharge = balancedReactants.Sum(r => r.Molecule.NetCharge * r.Coefficient);
                    int outCharge = balancedProducts.Sum(p => p.Molecule.NetCharge * p.Coefficient);
                    if (inCharge != outCharge) conserved = false;
                }

                if (conserved)
                {
                    pathways.Add(new Reaction(balancedReactants, balancedProducts));
                }
            }
        }

        return pathways;
    }

    /// <summary>
    /// Generates a structured 5-step educational balancing explanation with Markdown breakdown.
    /// </summary>
    public BalancedReactionWithSteps BalanceWithSteps() => StepByStepBalancer.BalanceWithSteps(this);

    /// <summary>
    /// Calculates reaction Enthalpy (ΔH), Entropy (ΔS), and Gibbs Free Energy (ΔG) at temperature T.
    /// </summary>
    /// <param name="temperatureKelvin">Temperature in Kelvin (default: 298.15 K).</param>
    public ReactionThermodynamicsResult GetThermodynamics(double temperatureKelvin = 298.15) =>
        ThermodynamicsEngine.GetThermodynamics(this, temperatureKelvin);

    /// <summary>Renders a resolution-independent vector SVG reaction diagram.</summary>
    public string ToSvg(bool isDarkMode = true) => SvgRenderer.RenderReactionSvg(this, isDarkMode);

    /// <summary>Saves a vector SVG reaction diagram directly to disk.</summary>
    public void SaveSvg(string filePath, bool isDarkMode = true) => File.WriteAllText(filePath, ToSvg(isDarkMode));

    /// <summary>Parses a chemical equation string (e.g. 'CH4 + O2 -> CO2 + H2O').</summary>
    public static Reaction Parse(string equation)
    {
        if (!TryParse(equation, out var reaction, out var error))
        {
            throw new FormatException($"Invalid reaction equation '{equation}': {error}");
        }

        return reaction;
    }

    /// <summary>Attempts to parse a chemical equation string.</summary>
    public static bool TryParse(string equation, [NotNullWhen(true)] out Reaction? reaction) =>
        TryParse(equation, out reaction, out _);

    /// <summary>Attempts to parse a chemical equation string, returning syntax error messages if invalid.</summary>
    public static bool TryParse(string equation, [NotNullWhen(true)] out Reaction? reaction, out string? errorMessage)
    {
        reaction = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(equation))
        {
            errorMessage = "Equation string is null or empty.";
            return false;
        }

        string[] sideSeparators = ["-->", "->", "=>", "=", "⇌"];
        string[] sides = Array.Empty<string>();

        foreach (var sep in sideSeparators)
        {
            if (equation.Contains(sep))
            {
                sides = equation.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                break;
            }
        }

        if (sides.Length != 2)
        {
            errorMessage = "Equation must contain a valid arrow separator ('->', '=', '=>', '-->', '⇌').";
            return false;
        }

        try
        {
            var reactants = ParseSide(sides[0]);
            var products = ParseSide(sides[1]);

            reaction = new Reaction(reactants, products);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>Parses one side (reactants or products) of a reaction equation.</summary>
    private static List<ReactionComponent> ParseSide(string sideStr)
    {
        var terms = SplitReactionTerms(sideStr);
        var components = new List<ReactionComponent>();

        foreach (var term in terms)
        {
            string trimmed = term.Trim();
            if (trimmed.Length == 0) continue;

            int i = 0;
            while (i < trimmed.Length && char.IsDigit(trimmed[i]))
            {
                i++;
            }

            int coefficient = 1;
            string formula = trimmed;

            if (i > 0)
            {
                coefficient = int.Parse(trimmed[..i], CultureInfo.InvariantCulture);
                formula = trimmed[i..].Trim();
            }

            var molecule = FormulaParser.Parse(formula);
            components.Add(new ReactionComponent(molecule, coefficient));
        }

        return components;
    }

    private static List<string> SplitReactionTerms(string sideStr)
    {
        var terms = new List<string>();
        int depth = 0;
        int lastStart = 0;

        for (int i = 0; i < sideStr.Length; i++)
        {
            char c = sideStr[i];
            if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']' or '}') depth = Math.Max(0, depth - 1);
            else if (c == '+' && depth == 0)
            {
                bool isSeparator = (i > 0 && char.IsWhiteSpace(sideStr[i - 1])) ||
                                   (i + 1 < sideStr.Length && char.IsWhiteSpace(sideStr[i + 1]));
                if (isSeparator)
                {
                    string term = sideStr[lastStart..i].Trim();
                    if (term.Length > 0) terms.Add(term);
                    lastStart = i + 1;
                }
            }
        }

        string finalTerm = sideStr[lastStart..].Trim();
        if (finalTerm.Length > 0) terms.Add(finalTerm);

        return terms;
    }

    /// <summary>Audits total count of atoms per element across components.</summary>
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

    /// <summary>Formats the reaction equation as a string.</summary>
    public override string ToString() =>
        $"{string.Join(" + ", Reactants)} -> {string.Join(" + ", Products)}";
}
