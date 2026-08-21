using System.Globalization;
using Chemy.Core.Spatial;
using Chemy.Core.Structure;

namespace Chemy.Core.IO;

/// <summary>
/// Parser and Deserializer for standard MDL Molfile (V2000) &amp; Structure-Data File (SDF) strings and files.
/// Reconstructs bonded molecular graphs, bond orders, formal charges (via atom-block charge codes and M  CHG records),
/// and 3D spatial coordinates with coordinate fidelity.
/// </summary>
public static class MolfileParser
{
    /// <summary>
    /// Parses an MDL Molfile V2000 string into a Molecule3D instance with verified bonded topology, 3D coordinates, and formal charges.
    /// </summary>
    public static Molecule3D FromMolfileV2000(string molfileContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(molfileContent);

        var lines = molfileContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        if (lines.Length < 4)
        {
            throw new FormatException("Invalid MDL Molfile: file contains fewer than 4 lines.");
        }

        string name = lines[0].Trim();
        if (string.IsNullOrEmpty(name)) name = "MolfileStructure";

        // Line 4: Counts line (aaabbb...)
        string countsLine = lines[3];
        if (countsLine.Length < 6)
        {
            throw new FormatException($"Invalid Molfile counts line: '{countsLine}'");
        }

        if (!int.TryParse(countsLine[..3].Trim(), CultureInfo.InvariantCulture, out int atomCount) ||
            !int.TryParse(countsLine[3..6].Trim(), CultureInfo.InvariantCulture, out int bondCount))
        {
            throw new FormatException($"Failed to parse atom and bond counts from line: '{countsLine}'");
        }

        int currentLine = 4;
        var rawPositions = new List<Vector3D>(atomCount);
        var rawElements = new List<Element>(atomCount);
        var rawCharges = new int[atomCount];

        for (int i = 0; i < atomCount; i++)
        {
            if (currentLine >= lines.Length)
            {
                throw new FormatException($"Unexpected end of file while reading atom record {i + 1}.");
            }

            string line = lines[currentLine++];
            if (line.Length < 34)
            {
                throw new FormatException($"Invalid atom block line length at atom {i + 1}: '{line}'");
            }

            double x = double.Parse(line[..10].Trim(), CultureInfo.InvariantCulture);
            double y = double.Parse(line[10..20].Trim(), CultureInfo.InvariantCulture);
            double z = double.Parse(line[20..30].Trim(), CultureInfo.InvariantCulture);
            string symbol = line[31..34].Trim();

            var element = Elements.GetBySymbol(symbol);

            int charge = 0;
            if (line.Length >= 39 && int.TryParse(line[36..39].Trim(), CultureInfo.InvariantCulture, out int cc))
            {
                charge = cc switch
                {
                    1 => +3,
                    2 => +2,
                    3 => +1,
                    5 => -1,
                    6 => -2,
                    7 => -3,
                    _ => 0
                };
            }

            rawPositions.Add(new Vector3D(x, y, z));
            rawElements.Add(element);
            rawCharges[i] = charge;
        }

        var bondList = new List<Bond>(bondCount);
        for (int i = 0; i < bondCount; i++)
        {
            if (currentLine >= lines.Length)
            {
                throw new FormatException($"Unexpected end of file while reading bond record {i + 1}.");
            }

            string line = lines[currentLine++];
            if (line.Length < 9)
            {
                throw new FormatException($"Invalid bond block line length at bond {i + 1}: '{line}'");
            }

            int atom1 = int.Parse(line[..3].Trim(), CultureInfo.InvariantCulture) - 1;
            int atom2 = int.Parse(line[3..6].Trim(), CultureInfo.InvariantCulture) - 1;
            int orderCode = int.Parse(line[6..9].Trim(), CultureInfo.InvariantCulture);

            if (atom1 < 0 || atom1 >= atomCount || atom2 < 0 || atom2 >= atomCount)
            {
                throw new FormatException($"Molfile bond endpoint index out of range: atom1={atom1 + 1}, atom2={atom2 + 1}, total atoms={atomCount}");
            }

            var bondType = orderCode switch
            {
                1 => BondType.Single,
                2 => BondType.Double,
                3 => BondType.Triple,
                4 => BondType.Aromatic,
                _ => throw new FormatException($"Unsupported or invalid MDL Molfile bond order code '{orderCode}' at bond {i + 1}.")
            };

            bondList.Add(new Bond(atom1, atom2, bondType));
        }

        // Properties Block: Process M  CHG and M  END
        while (currentLine < lines.Length)
        {
            string pLine = lines[currentLine++].Trim();
            if (pLine.StartsWith("M  END", StringComparison.Ordinal)) break;

            if (pLine.StartsWith("M  CHG", StringComparison.Ordinal))
            {
                var tokens = pLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 3 && int.TryParse(tokens[2], CultureInfo.InvariantCulture, out int count))
                {
                    int tokenIdx = 3;
                    for (int k = 0; k < count && tokenIdx + 1 < tokens.Length; k++)
                    {
                        if (int.TryParse(tokens[tokenIdx++], CultureInfo.InvariantCulture, out int aNum) &&
                            int.TryParse(tokens[tokenIdx++], CultureInfo.InvariantCulture, out int chg))
                        {
                            int aIdx = aNum - 1;
                            if (aIdx >= 0 && aIdx < atomCount)
                            {
                                rawCharges[aIdx] = chg;
                            }
                        }
                    }
                }
            }
        }

        // Construct final atoms and 3D representations with verified charges
        var atomList = new List<Atom>(atomCount);
        var atom3DList = new List<Atom3D>(atomCount);

        for (int i = 0; i < atomCount; i++)
        {
            var element = rawElements[i];
            int defaultNeutrons = Math.Max(0, (int)Math.Round(element.StandardAtomicMass) - element.AtomicNumber);
            int charge = rawCharges[i];
            int electrons = Math.Max(0, element.AtomicNumber - charge);

            var atom = new Atom(element, defaultNeutrons, electrons);
            atomList.Add(atom);
            atom3DList.Add(new Atom3D(atom, rawPositions[i]));
        }

        var sourceMol = new Molecule(name, atomList, bondList);
        return new Molecule3D(name, sourceMol.ChemicalFormula, "Conformer", 109.5, atom3DList, sourceMol);
    }

    /// <summary>
    /// Parses a multi-record Structure-Data File (.sdf) into a list of Molecule3D instances.
    /// </summary>
    public static IReadOnlyList<Molecule3D> FromSdf(string sdfContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sdfContent);

        var rawRecords = sdfContent.Split(["$$$$\r\n", "$$$$\r", "$$$$\n", "$$$$"], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<Molecule3D>(rawRecords.Length);

        foreach (var record in rawRecords)
        {
            string trimmed = record.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Extract the Molfile block (up to and including 'M  END')
            int mEndIndex = trimmed.IndexOf("M  END", StringComparison.Ordinal);
            if (mEndIndex >= 0)
            {
                string molBlock = trimmed[..(mEndIndex + 6)];
                result.Add(FromMolfileV2000(molBlock));
            }
            else
            {
                throw new FormatException("Invalid SDF record: missing required 'M  END' terminator block.");
            }
        }

        return result;
    }
}
