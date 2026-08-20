using System.Text;
using Chemy.Core.Spatial;

namespace Chemy.Core.Rendering;

/// <summary>
/// Renders standard IUPAC / ChemDraw 2D skeletal chemical structural diagrams in vector SVG.
/// Implements implicit carbon vertices, skeletal line bonds, parallel double bonds, ring concentric offsets,
/// and explicit heteroatom typography (OH, O, NH2, CH3, H3C, halogens).
/// </summary>
public static class SkeletalSvgRenderer
{
    public static string Render(Molecule molecule, bool isDarkMode = true, int width = 600, int height = 400, bool showTerminalMethyls = true)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        string bgColor = isDarkMode ? "#090d16" : "#ffffff";
        string bondColor = isDarkMode ? "#e2e8f0" : "#0f172a";

        var planar = molecule.ToPlanar3D();
        int nAtoms = planar.Atoms.Count;

        if (nAtoms == 0)
        {
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" width=\"{width}\" height=\"{height}\"><rect width=\"100%\" height=\"100%\" fill=\"{bgColor}\" rx=\"12\" /><text x=\"{width / 2}\" y=\"{height / 2}\" fill=\"#64748b\" text-anchor=\"middle\">Empty Molecule</text></svg>";
        }

        // Identify heavy atoms and determine which atoms should be explicitly labeled
        var isImplicitH = new bool[nAtoms];
        var atomLabels = new string?[nAtoms];
        var labelColors = new string[nAtoms];

        for (int i = 0; i < nAtoms; i++)
        {
            var atom = planar.Atoms[i].Atom;
            string sym = atom.Element.Symbol;

            if (sym == "H")
            {
                var parentBonds = molecule.Bonds.Where(b => b.Connects(i)).ToList();
                if (parentBonds.Count > 0)
                {
                    var parentBond = parentBonds[0];
                    int parentIdx = parentBond.Atom1Index == i ? parentBond.Atom2Index : parentBond.Atom1Index;
                    if (molecule.Atoms[parentIdx].Element.Symbol == "C")
                    {
                        isImplicitH[i] = true;
                    }
                }
            }
        }

        for (int i = 0; i < nAtoms; i++)
        {
            if (isImplicitH[i]) continue;

            var atom = planar.Atoms[i].Atom;
            string sym = atom.Element.Symbol;

            // Count attached hydrogens
            var attachedHs = molecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                .Where(nbr => molecule.Atoms[nbr].Element.Symbol == "H")
                .ToList();

            int hCount = attachedHs.Count;

            // Count attached heavy atoms
            var heavyNeighbors = molecule.Bonds
                .Where(b => b.Connects(i))
                .Select(b => b.Atom1Index == i ? b.Atom2Index : b.Atom1Index)
                .Where(nbr => molecule.Atoms[nbr].Element.Symbol != "H")
                .ToList();

            if (sym == "C")
            {
                if (heavyNeighbors.Count == 0)
                {
                    // Isolated methane
                    atomLabels[i] = "CH₄";
                    labelColors[i] = bondColor;
                }
                else if (heavyNeighbors.Count == 1 && showTerminalMethyls && hCount == 3)
                {
                    // Terminal methyl group: format as H3C if on the left, CH3 if on the right
                    var pPos = planar.Atoms[heavyNeighbors[0]].Position;
                    var cPos = planar.Atoms[i].Position;
                    atomLabels[i] = (cPos.X < pPos.X - 0.2) ? "H₃C" : "CH₃";
                    labelColors[i] = bondColor;
                }
                else
                {
                    // Internal carbon in chain or ring -> implicit vertex
                    atomLabels[i] = null;
                }
            }
            else if (sym != "H")
            {
                // Heteroatom: Oxygen, Nitrogen, Sulfur, Halogens, etc.
                labelColors[i] = sym switch
                {
                    "O" => isDarkMode ? "#f87171" : "#dc2626",
                    "N" => isDarkMode ? "#38bdf8" : "#0284c7",
                    "S" => isDarkMode ? "#facc15" : "#ca8a04",
                    "P" => isDarkMode ? "#fb923c" : "#ea580c",
                    "F" or "Cl" or "Br" or "I" => isDarkMode ? "#4ade80" : "#16a34a",
                    _ => bondColor
                };

                if (hCount == 0)
                {
                    atomLabels[i] = sym;
                }
                else if (hCount == 1)
                {
                    // If on left side, e.g. HO- vs -OH
                    var hNeighbor = heavyNeighbors.FirstOrDefault();
                    if (heavyNeighbors.Count > 0 && planar.Atoms[i].Position.X < planar.Atoms[hNeighbor].Position.X - 0.2)
                    {
                        atomLabels[i] = $"HO";
                    }
                    else
                    {
                        atomLabels[i] = $"{sym}H";
                    }
                }
                else
                {
                    atomLabels[i] = $"{sym}H{(hCount > 1 ? Subscript(hCount) : "")}";
                }
            }
        }

        // Calculate 2D bounding box of visible (non-implicit) atoms
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;

        int visibleCount = 0;
        for (int i = 0; i < nAtoms; i++)
        {
            if (isImplicitH[i]) continue;
            var pos = planar.Atoms[i].Position;
            if (pos.X < minX) minX = pos.X;
            if (pos.X > maxX) maxX = pos.X;
            if (pos.Y < minY) minY = pos.Y;
            if (pos.Y > maxY) maxY = pos.Y;
            visibleCount++;
        }

        if (visibleCount == 0)
        {
            minX = -1; maxX = 1; minY = -1; maxY = 1;
        }

        double spanX = Math.Max(0.5, maxX - minX);
        double spanY = Math.Max(0.5, maxY - minY);

        double padding = 60.0;
        double drawW = width - (2.0 * padding);
        double drawH = height - (2.0 * padding);

        double scale = Math.Min(drawW / spanX, drawH / spanY);
        double offsetX = (width - (spanX * scale)) / 2.0;
        double offsetY = (height - (spanY * scale)) / 2.0;

        // Map Cartesian (X, Y) to SVG viewport coordinates (inverting Y)
        var svgCoords = new (double X, double Y)[nAtoms];
        for (int i = 0; i < nAtoms; i++)
        {
            var p = planar.Atoms[i].Position;
            double sx = offsetX + ((p.X - minX) * scale);
            double sy = height - (offsetY + ((p.Y - minY) * scale));
            svgCoords[i] = (sx, sy);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" width=\"100%\" height=\"100%\" style=\"max-height: 100%;\">");
        sb.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"{bgColor}\" rx=\"8\" />");

        // 1. Draw Chemical Bonds
        foreach (var bond in molecule.Bonds)
        {
            int u = bond.Atom1Index;
            int v = bond.Atom2Index;

            if (isImplicitH[u] || isImplicitH[v]) continue;

            var (x1, y1) = svgCoords[u];
            var (x2, y2) = svgCoords[v];

            double dx = x2 - x1;
            double dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-4) continue;

            double nx = -dy / len;
            double ny = dx / len;

            if (bond.Type == BondType.Double)
            {
                double d = 2.6;
                sb.AppendLine($"  <line x1=\"{x1 + nx * d:F1}\" y1=\"{y1 + ny * d:F1}\" x2=\"{x2 + nx * d:F1}\" y2=\"{y2 + ny * d:F1}\" stroke=\"{bondColor}\" stroke-width=\"2.2\" stroke-linecap=\"round\" />");
                sb.AppendLine($"  <line x1=\"{x1 - nx * d:F1}\" y1=\"{y1 - ny * d:F1}\" x2=\"{x2 - nx * d:F1}\" y2=\"{y2 - ny * d:F1}\" stroke=\"{bondColor}\" stroke-width=\"2.2\" stroke-linecap=\"round\" />");
            }
            else if (bond.Type == BondType.Triple)
            {
                double d = 3.5;
                sb.AppendLine($"  <line x1=\"{x1:F1}\" y1=\"{y1:F1}\" x2=\"{x2:F1}\" y2=\"{y2:F1}\" stroke=\"{bondColor}\" stroke-width=\"2.0\" stroke-linecap=\"round\" />");
                sb.AppendLine($"  <line x1=\"{x1 + nx * d:F1}\" y1=\"{y1 + ny * d:F1}\" x2=\"{x2 + nx * d:F1}\" y2=\"{y2 + ny * d:F1}\" stroke=\"{bondColor}\" stroke-width=\"1.8\" stroke-linecap=\"round\" />");
                sb.AppendLine($"  <line x1=\"{x1 - nx * d:F1}\" y1=\"{y1 - ny * d:F1}\" x2=\"{x2 - nx * d:F1}\" y2=\"{y2 - ny * d:F1}\" stroke=\"{bondColor}\" stroke-width=\"1.8\" stroke-linecap=\"round\" />");
            }
            else if (bond.Type == BondType.Aromatic)
            {
                // Aromatic bond: primary line + inner concentric dash
                double d = 2.4;
                sb.AppendLine($"  <line x1=\"{x1:F1}\" y1=\"{y1:F1}\" x2=\"{x2:F1}\" y2=\"{y2:F1}\" stroke=\"{bondColor}\" stroke-width=\"2.4\" stroke-linecap=\"round\" />");
                sb.AppendLine($"  <line x1=\"{x1 + nx * d:F1}\" y1=\"{y1 + ny * d:F1}\" x2=\"{x2 + nx * d:F1}\" y2=\"{y2 + ny * d:F1}\" stroke=\"{bondColor}\" stroke-width=\"1.6\" stroke-dasharray=\"4,3\" stroke-linecap=\"round\" />");
            }
            else
            {
                // Single bond
                sb.AppendLine($"  <line x1=\"{x1:F1}\" y1=\"{y1:F1}\" x2=\"{x2:F1}\" y2=\"{y2:F1}\" stroke=\"{bondColor}\" stroke-width=\"2.4\" stroke-linecap=\"round\" />");
            }
        }

        // 2. Draw Atom Labels with Background Knockout
        for (int i = 0; i < nAtoms; i++)
        {
            string? label = atomLabels[i];
            if (label == null) continue;

            var (sx, sy) = svgCoords[i];
            string col = labelColors[i];

            double textW = label.Length * 11.0 + 8.0;
            double textH = 22.0;

            // Background knockout rectangle so lines don't cross text
            sb.AppendLine($"  <rect x=\"{sx - (textW / 2.0):F1}\" y=\"{sy - (textH / 2.0):F1}\" width=\"{textW:F1}\" height=\"{textH:F1}\" fill=\"{bgColor}\" rx=\"4\" />");
            sb.AppendLine($"  <text x=\"{sx:F1}\" y=\"{sy + 5.5:F1}\" fill=\"{col}\" font-family=\"system-ui, -apple-system, sans-serif\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\">{label}</text>");
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string Subscript(int n) => n switch
    {
        2 => "₂",
        3 => "₃",
        4 => "₄",
        5 => "₅",
        6 => "₆",
        _ => n.ToString()
    };
}
