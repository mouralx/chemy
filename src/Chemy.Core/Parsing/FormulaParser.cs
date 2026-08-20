using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Chemy.Core.Parsing;

public static class FormulaParser
{
    private static readonly Regex ChargeRegex = new(@"(?:\^)?([+-]?\d*|\d+[+-]|[+-])\s*$", RegexOptions.Compiled);

    public static Molecule Parse(string formula, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formula);

        if (!TryParse(formula, name, out var result, out var errorMessage))
        {
            throw new FormatException($"Invalid chemical formula '{formula}': {errorMessage}");
        }

        return result;
    }

    public static bool TryParse(string formula, [NotNullWhen(true)] out Molecule? result) =>
        TryParse(formula, null, out result, out _);

    public static bool TryParse(string formula, string? name, [NotNullWhen(true)] out Molecule? result) =>
        TryParse(formula, name, out result, out _);

    public static bool TryParse(string formula, string? name, [NotNullWhen(true)] out Molecule? result, out string? errorMessage)
    {
        result = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(formula))
        {
            errorMessage = "Formula string is null or empty.";
            return false;
        }

        try
        {
            string cleanFormula = ExtractCharge(formula.Trim(), out int charge);
            string[] components = cleanFormula.Split(['*', '.'], StringSplitOptions.RemoveEmptyEntries);

            if (components.Length == 0)
            {
                errorMessage = "Formula contains no valid components.";
                return false;
            }

            var elementCounts = new Dictionary<Element, int>();

            foreach (var component in components)
            {
                string trimmedComp = component.Trim();
                if (trimmedComp.Length == 0) continue;

                int leadingMultiplier = 1;
                int i = 0;
                while (i < trimmedComp.Length && char.IsDigit(trimmedComp[i]))
                {
                    i++;
                }

                if (i > 0)
                {
                    leadingMultiplier = int.Parse(trimmedComp[..i], CultureInfo.InvariantCulture);
                    trimmedComp = trimmedComp[i..].Trim();
                }

                if (string.IsNullOrEmpty(trimmedComp))
                {
                    errorMessage = "Invalid component with multiplier but no chemical formula.";
                    return false;
                }

                var compCounts = ParseSingleFormula(trimmedComp);
                foreach (var (element, count) in compCounts)
                {
                    elementCounts[element] = elementCounts.GetValueOrDefault(element, 0) + count * leadingMultiplier;
                }
            }

            if (elementCounts.Count == 0)
            {
                errorMessage = "No elements found in formula.";
                return false;
            }

            var atoms = new List<Atom>();
            foreach (var (element, count) in elementCounts)
            {
                int defaultNeutrons = Math.Max(0, (int)Math.Round(element.StandardAtomicMass) - element.AtomicNumber);
                for (int c = 0; c < count; c++)
                {
                    atoms.Add(new Atom(element, defaultNeutrons));
                }
            }

            if (charge != 0 && atoms.Count > 0)
            {
                atoms[^1] = atoms[^1].Ionize(charge);
            }

            var bonds = AutoGenerateBonds(atoms);

            result = new Molecule(name ?? formula, atoms, bonds);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static List<Bond> AutoGenerateBonds(List<Atom> atoms)
    {
        var bonds = new List<Bond>();
        if (atoms.Count <= 1) return bonds;

        var centerAtom = atoms.FirstOrDefault(a => a.Element.Symbol != "H") ?? atoms[0];
        int centerIndex = atoms.IndexOf(centerAtom);

        for (int i = 0; i < atoms.Count; i++)
        {
            if (i != centerIndex)
            {
                bonds.Add(new Bond(centerIndex, i, BondType.Single));
            }
        }

        return bonds;
    }

    private static Dictionary<Element, int> ParseSingleFormula(string formula)
    {
        var stack = new Stack<Dictionary<Element, int>>();
        stack.Push([]);

        int i = 0;
        int len = formula.Length;

        while (i < len)
        {
            char ch = formula[i];

            if (ch is '(' or '[' or '{')
            {
                stack.Push([]);
                i++;
            }
            else if (ch is ')' or ']' or '}')
            {
                if (stack.Count <= 1)
                {
                    throw new FormatException($"Unmatched closing bracket '{ch}' at position {i}.");
                }

                var top = stack.Pop();
                i++;

                int multiplier = ReadNumber(formula, ref i);
                if (multiplier == 0) multiplier = 1;

                var parent = stack.Peek();
                foreach (var (element, count) in top)
                {
                    parent[element] = parent.GetValueOrDefault(element, 0) + count * multiplier;
                }
            }
            else if (char.IsUpper(ch))
            {
                int start = i;
                i++;
                while (i < len && char.IsLower(formula[i]))
                {
                    i++;
                }

                string symbol = formula[start..i];
                Element element;
                try
                {
                    element = Elements.GetBySymbol(symbol);
                }
                catch (KeyNotFoundException)
                {
                    throw new FormatException($"Unknown element symbol '{symbol}' at position {start}.");
                }

                int count = ReadNumber(formula, ref i);
                if (count == 0) count = 1;

                var currentDict = stack.Peek();
                currentDict[element] = currentDict.GetValueOrDefault(element, 0) + count;
            }
            else
            {
                throw new FormatException($"Unexpected character '{ch}' at position {i}.");
            }
        }

        if (stack.Count > 1)
        {
            throw new FormatException("Unmatched opening bracket in formula.");
        }

        return stack.Pop();
    }

    private static int ReadNumber(string formula, ref int index)
    {
        int start = index;
        while (index < formula.Length && char.IsDigit(formula[index]))
        {
            index++;
        }

        return start == index ? 0 : int.Parse(formula[start..index], CultureInfo.InvariantCulture);
    }

    private static string ExtractCharge(string formula, out int charge)
    {
        charge = 0;

        var match = ChargeRegex.Match(formula);
        if (!match.Success || string.IsNullOrEmpty(match.Groups[1].Value))
        {
            return formula;
        }

        string chargeStr = match.Groups[1].Value;
        int matchIndex = match.Index;

        if (!chargeStr.Contains('^') && !chargeStr.Contains('+') && !chargeStr.Contains('-'))
        {
            return formula;
        }

        if (TryParseChargeString(chargeStr, out int parsedCharge))
        {
            charge = parsedCharge;
            return formula[..matchIndex].Trim();
        }

        return formula;
    }

    private static bool TryParseChargeString(string s, out int charge)
    {
        charge = 0;
        s = s.Trim();

        if (s is "+" or "^+") { charge = 1; return true; }
        if (s is "-" or "^-") { charge = -1; return true; }

        if (s.EndsWith('+'))
        {
            if (int.TryParse(s[..^1], out int val)) { charge = val; return true; }
        }
        else if (s.EndsWith('-'))
        {
            if (int.TryParse(s[..^1], out int val)) { charge = -val; return true; }
        }
        else if (s.StartsWith('+'))
        {
            if (int.TryParse(s[1..], out int val)) { charge = val; return true; }
        }
        else if (s.StartsWith('-'))
        {
            if (int.TryParse(s[1..], out int val)) { charge = -val; return true; }
        }

        return false;
    }
}
