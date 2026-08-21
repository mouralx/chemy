using System.Text;

namespace Chemy.Core.Structure;

public static class SmilesParser
{
    private sealed record RawAtom(Element Element, int ExplicitCharge, int ExplicitH = -1, bool IsAromatic = false);

    public static Molecule Parse(string smiles, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(smiles);

        var rawAtoms = new List<RawAtom>();
        var rawBonds = new List<(int Atom1, int Atom2, BondType Type)>();
        var ringOpenings = new Dictionary<int, (int AtomIndex, BondType BondType, bool ExplicitBond)>();
        var branchStack = new Stack<int>();

        int currentAtomIndex = -1;
        int i = 0;
        int len = smiles.Length;
        BondType currentBondType = BondType.Single;
        bool explicitBondSpecified = false;

        while (i < len)
        {
            char ch = smiles[i];

            if (ch == '(')
            {
                branchStack.Push(currentAtomIndex);
                i++;
                currentBondType = BondType.Single;
                explicitBondSpecified = false;
            }
            else if (ch == ')')
            {
                if (branchStack.Count > 0) currentAtomIndex = branchStack.Pop();
                i++;
                currentBondType = BondType.Single;
                explicitBondSpecified = false;
            }
            else if (ch is '=' or '#' or ':' or '-')
            {
                currentBondType = ch switch
                {
                    '=' => BondType.Double,
                    '#' => BondType.Triple,
                    ':' => BondType.Aromatic,
                    _ => BondType.Single
                };
                explicitBondSpecified = true;
                i++;
            }
            else if (char.IsDigit(ch))
            {
                int ringId = ch - '0';
                if (ringOpenings.TryGetValue(ringId, out var open))
                {
                    bool bothAromatic = rawAtoms[currentAtomIndex].IsAromatic && rawAtoms[open.AtomIndex].IsAromatic;
                    var ringBondType = open.ExplicitBond
                        ? open.BondType 
                        : (explicitBondSpecified ? currentBondType : (bothAromatic ? BondType.Aromatic : BondType.Single));
                    rawBonds.Add((open.AtomIndex, currentAtomIndex, ringBondType));
                    ringOpenings.Remove(ringId);
                }
                else
                {
                    ringOpenings[ringId] = (currentAtomIndex, currentBondType, explicitBondSpecified);
                }
                i++;
                currentBondType = BondType.Single;
                explicitBondSpecified = false;
            }
            else if (ch == '[')
            {
                i++;
                int start = i;
                while (i < len && smiles[i] != ']') i++;

                string bracketContent = smiles[start..i];
                i++; // skip ']'

                var (element, charge, explicitH, isAromatic) = ParseBracketAtom(bracketContent);
                rawAtoms.Add(new RawAtom(element, charge, explicitH, isAromatic));
                int newIndex = rawAtoms.Count - 1;

                if (currentAtomIndex >= 0)
                {
                    bool bothAromatic = isAromatic && rawAtoms[currentAtomIndex].IsAromatic;
                    var bondType = explicitBondSpecified
                        ? currentBondType
                        : (bothAromatic ? BondType.Aromatic : BondType.Single);
                    rawBonds.Add((currentAtomIndex, newIndex, bondType));
                }

                currentAtomIndex = newIndex;
                currentBondType = BondType.Single;
                explicitBondSpecified = false;
            }
            else if (ch == '.')
            {
                currentAtomIndex = -1;
                branchStack.Clear();
                currentBondType = BondType.Single;
                explicitBondSpecified = false;
                i++;
            }
            else if (char.IsLetter(ch))
            {
                string symbol = ch.ToString();
                bool isAromatic = char.IsLower(ch);

                if (i + 1 < len && char.IsLower(smiles[i + 1]) && !isAromatic)
                {
                    string candidate = smiles.Substring(i, 2);
                    if (candidate is "Cl" or "Br")
                    {
                        symbol = candidate;
                        i++;
                    }
                }

                if (isAromatic) symbol = char.ToUpper(symbol[0]).ToString();

                var element = Elements.GetBySymbol(symbol);
                rawAtoms.Add(new RawAtom(element, 0, -1, isAromatic));
                int newIndex = rawAtoms.Count - 1;

                if (currentAtomIndex >= 0)
                {
                    bool bothAromatic = isAromatic && rawAtoms[currentAtomIndex].IsAromatic;
                    var bondType = explicitBondSpecified
                        ? currentBondType 
                        : (bothAromatic ? BondType.Aromatic : BondType.Single);
                    rawBonds.Add((currentAtomIndex, newIndex, bondType));
                }

                currentAtomIndex = newIndex;
                currentBondType = BondType.Single;
                explicitBondSpecified = false;
                i++;
            }
            else if (ch is '@' or '/' or '\\' or '%')
            {
                throw new NotSupportedException($"SMILES character '{ch}' at position {i} (stereochemistry or extended ring closure) is not supported in the 2D topological graph parser.");
            }
            else
            {
                throw new FormatException($"Invalid or unsupported character '{ch}' at position {i} in SMILES string '{smiles}'.");
            }
        }

        var finalAtoms = new List<Atom>();
        var finalBonds = new List<Bond>();

        foreach (var bond in rawBonds)
        {
            finalBonds.Add(new Bond(bond.Atom1, bond.Atom2, bond.Type));
        }

        for (int aIdx = 0; aIdx < rawAtoms.Count; aIdx++)
        {
            var raw = rawAtoms[aIdx];
            int defaultNeutrons = Math.Max(0, (int)Math.Round(raw.Element.StandardAtomicMass) - raw.Element.AtomicNumber);

            var atom = new Atom(raw.Element, defaultNeutrons);
            if (raw.ExplicitCharge != 0) atom = atom.Ionize(raw.ExplicitCharge);

            finalAtoms.Add(atom);
        }

        int originalAtomCount = finalAtoms.Count;
        for (int aIdx = 0; aIdx < originalAtomCount; aIdx++)
        {
            var raw = rawAtoms[aIdx];
            int hCount = raw.ExplicitH;

            if (hCount < 0)
            {
                double currentBondOrder = 0;
                foreach (var b in finalBonds)
                {
                    if (b.Connects(aIdx))
                    {
                        currentBondOrder += b.Type switch
                        {
                            BondType.Double => 2.0,
                            BondType.Triple => 3.0,
                            BondType.Aromatic => 1.5,
                            _ => 1.0
                        };
                    }
                }

                int defaultValence = GetDefaultValence(raw.Element.Symbol, currentBondOrder, raw.IsAromatic);
                hCount = Math.Max(0, (int)Math.Round(defaultValence - currentBondOrder));
            }

            int hDefaultNeutrons = Math.Max(0, (int)Math.Round(Elements.Hydrogen.StandardAtomicMass) - Elements.Hydrogen.AtomicNumber);
            for (int h = 0; h < hCount; h++)
            {
                finalAtoms.Add(new Atom(Elements.Hydrogen, hDefaultNeutrons));
                int hIndex = finalAtoms.Count - 1;
                finalBonds.Add(new Bond(aIdx, hIndex, BondType.Single));
            }
        }

        return new Molecule(name ?? smiles, finalAtoms, finalBonds);
    }

    private static (Element Element, int Charge, int ExplicitH, bool IsAromatic) ParseBracketAtom(string content)
    {
        if (content.Contains('@') || content.Contains('/') || content.Contains('\\'))
        {
            throw new NotSupportedException($"Stereochemical descriptor in bracket atom '[{content}]' is not currently supported.");
        }

        int i = 0;
        int len = content.Length;

        while (i < len && !char.IsLetter(content[i])) i++;

        int startSymbol = i;
        bool isAromatic = false;
        if (i < len && char.IsUpper(content[i]))
        {
            i++;
            if (i < len && char.IsLower(content[i])) i++;
        }
        else if (i < len && char.IsLower(content[i]))
        {
            isAromatic = true;
            i++;
        }

        string symbolStr = content.Substring(startSymbol, i - startSymbol);
        if (symbolStr.Length == 1 && char.IsLower(symbolStr[0])) symbolStr = char.ToUpper(symbolStr[0]).ToString();

        var element = Elements.GetBySymbol(symbolStr);
        int explicitH = 0; // In OpenSMILES bracket atoms, hydrogen count defaults to 0 unless 'H' is specified
        int charge = 0;

        if (i < len && (content[i] == 'H' || content[i] == 'h'))
        {
            i++;
            int startH = i;
            while (i < len && char.IsDigit(content[i])) i++;
            explicitH = startH == i ? 1 : int.Parse(content.Substring(startH, i - startH));
        }

        if (i < len && (content[i] is '+' or '-'))
        {
            char sign = content[i];
            i++;
            int startC = i;
            while (i < len && char.IsDigit(content[i])) i++;
            int val = startC == i ? 1 : int.Parse(content.Substring(startC, i - startC));
            charge = sign == '+' ? val : -val;
        }

        return (element, charge, explicitH, isAromatic);
    }

    private static int GetDefaultValence(string symbol, double currentBondOrder, bool isAromatic) => symbol switch
    {
        "C" => 4,
        "N" => currentBondOrder > 3.0 ? 4 : 3,
        "O" => 2,
        "S" => isAromatic ? 3 : (currentBondOrder > 4.0 ? 6 : (currentBondOrder > 2.0 ? 4 : 2)),
        "P" => currentBondOrder > 3.0 ? 5 : 3,
        "F" or "Cl" or "Br" or "I" => 1,
        _ => 0
    };

    public static bool TryParse(string smiles, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Molecule? molecule) =>
        TryParse(smiles, null, out molecule, out _);

    public static bool TryParse(string smiles, string? name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Molecule? molecule, out string? errorMessage)
    {
        molecule = null;
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(smiles))
        {
            errorMessage = "SMILES string cannot be null or whitespace.";
            return false;
        }

        try
        {
            molecule = Parse(smiles, name);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static bool IsKnownSymbol(string symbol) => Elements.TryGetBySymbol(symbol, out _);
}

