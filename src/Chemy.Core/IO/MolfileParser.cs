using System.Globalization;
using Chemy.Core.Spatial;
using Chemy.Core.Structure;

namespace Chemy.Core.IO;

/// <summary>
/// Parser and Deserializer for standard MDL Molfile (V2000) strings and files.
/// Reconstructs bonded molecular graphs and 3D spatial coordinates.
/// </summary>
public static class MolfileParser
{
    /// <summary>
    /// Parses an MDL Molfile V2000 string into a Molecule3D instance with verified bonded topology.
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
        var atom3DList = new List<Atom3D>(atomCount);
        var atomList = new List<Atom>(atomCount);

        for (int i = 0; i < atomCount; i++)
        {
            if (currentLine >= lines.Length)
            {
                throw new FormatException($"Unexpected end of file while reading atom record {i + 1}.");
            }

            string line = lines[currentLine++];
            if (line.Length < 34)
            {
                throw new FormatException($"Invalid atom block line length: '{line}'");
            }

            double x = double.Parse(line[..10].Trim(), CultureInfo.InvariantCulture);
            double y = double.Parse(line[10..20].Trim(), CultureInfo.InvariantCulture);
            double z = double.Parse(line[20..30].Trim(), CultureInfo.InvariantCulture);
            string symbol = line[31..34].Trim();

            var element = Elements.GetBySymbol(symbol);
            int defaultNeutrons = Math.Max(0, (int)Math.Round(element.StandardAtomicMass) - element.AtomicNumber);
            var atom = new Atom(element, defaultNeutrons);

            atomList.Add(atom);
            atom3DList.Add(new Atom3D(atom, new Vector3D(x, y, z)));
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
                throw new FormatException($"Invalid bond block line length: '{line}'");
            }

            int atom1 = int.Parse(line[..3].Trim(), CultureInfo.InvariantCulture) - 1;
            int atom2 = int.Parse(line[3..6].Trim(), CultureInfo.InvariantCulture) - 1;
            int orderCode = int.Parse(line[6..9].Trim(), CultureInfo.InvariantCulture);

            var bondType = orderCode switch
            {
                1 => BondType.Single,
                2 => BondType.Double,
                3 => BondType.Triple,
                4 => BondType.Aromatic,
                _ => BondType.Single
            };

            bondList.Add(new Bond(atom1, atom2, bondType));
        }

        var sourceMol = new Molecule(name, atomList, bondList);
        return new Molecule3D(name, sourceMol.ChemicalFormula, "Conformer", 109.5, atom3DList, sourceMol);
    }
}
