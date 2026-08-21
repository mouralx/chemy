using System.Globalization;
using System.Text;
using Chemy.Core.Spatial;
using Chemy.Core.Structure;

namespace Chemy.Core.IO;

/// <summary>
/// Industrial-Grade MDL Molfile (V2000) &amp; Structure-Data File (SDF) Serializer.
/// Produces ISO/IUPAC-compliant chemical structure files compatible with ChemDraw, PyMOL, RDKit, and BIOVIA Discovery Studio.
/// </summary>
public static class MolfileExporter
{
    /// <summary>
    /// Exports a Molecule3D instance into standard MDL Molfile V2000 format.
    /// </summary>
    /// <param name="molecule3D">3D molecular structure with Cartesian coordinates.</param>
    /// <returns>Formatted MDL Molfile V2000 string.</returns>
    public static string ToMolfileV2000(Molecule3D molecule3D)
    {
        ArgumentNullException.ThrowIfNull(molecule3D);

        var sb = new StringBuilder();

        // Header Block (3 lines)
        sb.AppendLine(molecule3D.Name);
        sb.AppendLine("  Chemy10 08202600002D 1   1.00000     0.00000     0");
        sb.AppendLine("Computational Chemistry Studio V2000");

        int atomCount = molecule3D.Atoms.Count;
        int bondCount = molecule3D.SourceMolecule.Bonds.Count;

        // Counts Line: aaabbblllfffcccsssxxxrrrpppiii999 V2000
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,3}{1,3}  0  0  0  0  0  0  0  0999 V2000", atomCount, bondCount));

        // Atom Block
        foreach (var atom3D in molecule3D.Atoms)
        {
            int chargeCode = atom3D.Atom.NetCharge switch
            {
                +3 => 1,
                +2 => 2,
                +1 => 3,
                -1 => 5,
                -2 => 6,
                -3 => 7,
                _ => 0
            };

            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,10:F4}{1,10:F4}{2,10:F4} {3,-3} 0{4,3}  0  0  0  0  0  0  0  0  0  0",
                atom3D.Position.X,
                atom3D.Position.Y,
                atom3D.Position.Z,
                atom3D.Atom.Element.Symbol,
                chargeCode
            ));
        }

        // Bond Block (1-indexed)
        foreach (var bond in molecule3D.SourceMolecule.Bonds)
        {
            int bondOrder = bond.Type switch
            {
                BondType.Single => 1,
                BondType.Double => 2,
                BondType.Triple => 3,
                BondType.Aromatic => 4,
                _ => 1
            };

            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,3}{1,3}{2,3}  0  0  0  0",
                bond.Atom1Index + 1,
                bond.Atom2Index + 1,
                bondOrder
            ));
        }

        // Properties Block: M  CHG
        var chargedAtoms = molecule3D.Atoms
            .Select((a, idx) => (Index: idx + 1, Charge: a.Atom.NetCharge))
            .Where(x => x.Charge != 0)
            .ToList();

        if (chargedAtoms.Count > 0)
        {
            for (int i = 0; i < chargedAtoms.Count; i += 8)
            {
                var chunk = chargedAtoms.Skip(i).Take(8).ToList();
                var chgSb = new StringBuilder();
                chgSb.Append(string.Format(CultureInfo.InvariantCulture, "M  CHG{0,3}", chunk.Count));
                foreach (var (idx, chg) in chunk)
                {
                    chgSb.Append(string.Format(CultureInfo.InvariantCulture, "{0,4}{1,4}", idx, chg));
                }
                sb.AppendLine(chgSb.ToString());
            }
        }

        sb.AppendLine("M  END");
        return sb.ToString();
    }

    /// <summary>
    /// Exports a collection of molecules into a multi-record Structure-Data File (.sdf).
    /// </summary>
    public static string ToSdf(IEnumerable<Molecule3D> molecules)
    {
        ArgumentNullException.ThrowIfNull(molecules);

        var sb = new StringBuilder();
        foreach (var mol in molecules)
        {
            sb.Append(ToMolfileV2000(mol));
            sb.AppendLine($"> <FORMULA>\n{mol.ChemicalFormula}\n");
            sb.AppendLine($"> <VSEPR_SHAPE>\n{mol.VseprShape}\n");
            sb.AppendLine("$$$$");
        }

        return sb.ToString();
    }
}
