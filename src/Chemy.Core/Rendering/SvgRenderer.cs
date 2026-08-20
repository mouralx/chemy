using System.Text;
using Chemy.Core.Thermodynamics;

namespace Chemy.Core.Rendering;

public static class SvgRenderer
{
    public static string RenderMoleculeSvg(Molecule molecule, bool isDarkMode = true)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        string bgColor = isDarkMode ? "#1e1e2e" : "#ffffff";
        string textColor = isDarkMode ? "#cdd6f4" : "#111827";
        string subTextColor = isDarkMode ? "#a6adc8" : "#4b5563";
        string cardBg = isDarkMode ? "#313244" : "#f3f4f6";
        string accentColor = isDarkMode ? "#89b4fa" : "#2563eb";

        var elementCounts = molecule.Atoms
            .GroupBy(a => a.Element)
            .Select(g => (Element: g.Key, Count: g.Count()))
            .OrderBy(x => x.Element.AtomicNumber)
            .ToList();

        var sb = new StringBuilder();
        int width = 500;
        int height = 220;

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" width=\"{width}\" height=\"{height}\">");
        sb.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"{bgColor}\" rx=\"12\" />");
        sb.AppendLine($"  <rect x=\"16\" y=\"16\" width=\"{width - 32}\" height=\"{height - 32}\" fill=\"{cardBg}\" rx=\"8\" />");

        sb.AppendLine($"  <text x=\"32\" y=\"50\" fill=\"{textColor}\" font-family=\"system-ui, sans-serif\" font-size=\"20\" font-weight=\"bold\">{EscapeXml(molecule.Name)}</text>");
        sb.AppendLine($"  <text x=\"32\" y=\"78\" fill=\"{accentColor}\" font-family=\"system-ui, sans-serif\" font-size=\"24\" font-weight=\"600\">{EscapeXml(molecule.ChemicalFormula)}</text>");

        sb.AppendLine($"  <text x=\"{width - 32}\" y=\"50\" fill=\"{subTextColor}\" font-family=\"system-ui, sans-serif\" font-size=\"14\" text-anchor=\"end\">{molecule.MolecularWeight.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} g/mol</text>");
        if (molecule.NetCharge != 0)
        {
            string chargeStr = molecule.NetCharge > 0 ? $"+{molecule.NetCharge}" : $"{molecule.NetCharge}";
            sb.AppendLine($"  <text x=\"{width - 32}\" y=\"78\" fill=\"#f38ba8\" font-family=\"system-ui, sans-serif\" font-size=\"14\" font-weight=\"bold\" text-anchor=\"end\">Charge: {chargeStr}</text>");
        }

        sb.AppendLine($"  <line x1=\"32\" y1=\"96\" x2=\"{width - 32}\" y2=\"96\" stroke=\"{subTextColor}\" stroke-opacity=\"0.3\" stroke-width=\"1\" />");

        sb.AppendLine($"  <text x=\"32\" y=\"125\" fill=\"{subTextColor}\" font-family=\"system-ui, sans-serif\" font-size=\"12\" font-weight=\"600\" letter-spacing=\"1\">COMPOSITION</text>");

        int badgeX = 32;
        int badgeY = 140;
        foreach (var (element, count) in elementCounts)
        {
            string elemColor = GetElementColor(element.Symbol, isDarkMode);
            string label = $"{element.Symbol}: {count}";
            int badgeWidth = label.Length * 9 + 20;

            if (badgeX + badgeWidth > width - 32)
            {
                badgeX = 32;
                badgeY += 32;
            }

            sb.AppendLine($"  <rect x=\"{badgeX}\" y=\"{badgeY}\" width=\"{badgeWidth}\" height=\"26\" fill=\"{elemColor}\" rx=\"13\" fill-opacity=\"0.2\" stroke=\"{elemColor}\" stroke-width=\"1\" />");
            sb.AppendLine($"  <text x=\"{badgeX + badgeWidth / 2}\" y=\"{badgeY + 17}\" fill=\"{elemColor}\" font-family=\"system-ui, sans-serif\" font-size=\"12\" font-weight=\"bold\" text-anchor=\"middle\">{label}</text>");

            badgeX += badgeWidth + 8;
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    public static string RenderReactionSvg(Reaction reaction, bool isDarkMode = true)
    {
        ArgumentNullException.ThrowIfNull(reaction);

        var balanced = reaction.IsBalanced ? reaction : reaction.Balance();
        ReactionThermodynamicsResult? thermo = null;
        try
        {
            thermo = balanced.GetThermodynamics();
        }
        catch
        {
        }

        string bgColor = isDarkMode ? "#1e1e2e" : "#ffffff";
        string textColor = isDarkMode ? "#cdd6f4" : "#111827";
        string subTextColor = isDarkMode ? "#a6adc8" : "#4b5563";
        string cardBg = isDarkMode ? "#313244" : "#f3f4f6";
        string accentColor = isDarkMode ? "#a6e3a1" : "#16a34a";

        var sb = new StringBuilder();
        int width = 700;
        int height = 180;

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" width=\"{width}\" height=\"{height}\">");
        sb.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"{bgColor}\" rx=\"12\" />");
        sb.AppendLine($"  <rect x=\"16\" y=\"16\" width=\"{width - 32}\" height=\"{height - 32}\" fill=\"{cardBg}\" rx=\"8\" />");

        sb.AppendLine($"  <text x=\"32\" y=\"45\" fill=\"{subTextColor}\" font-family=\"system-ui, sans-serif\" font-size=\"12\" font-weight=\"600\" letter-spacing=\"1\">BALANCED CHEMICAL REACTION</text>");
        sb.AppendLine($"  <text x=\"32\" y=\"90\" fill=\"{textColor}\" font-family=\"system-ui, sans-serif\" font-size=\"24\" font-weight=\"bold\">{EscapeXml(balanced.ToString())}</text>");

        if (thermo != null)
        {
            string thermoTag = $"ΔH = {thermo.EnthalpyChangekJ:F1} kJ/mol | ΔG = {thermo.GibbsFreeEnergykJ:F1} kJ/mol | {(thermo.IsExothermic ? "Exothermic" : "Endothermic")}";
            sb.AppendLine($"  <rect x=\"32\" y=\"115\" width=\"{width - 64}\" height=\"28\" fill=\"{accentColor}\" fill-opacity=\"0.15\" rx=\"6\" stroke=\"{accentColor}\" stroke-width=\"1\" />");
            sb.AppendLine($"  <text x=\"44\" y=\"134\" fill=\"{accentColor}\" font-family=\"system-ui, sans-serif\" font-size=\"12\" font-weight=\"bold\">{thermoTag}</text>");
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string GetElementColor(string symbol, bool isDarkMode) => symbol switch
    {
        "H" => "#f5e0dc",
        "C" => isDarkMode ? "#cba6f7" : "#8b5cf6",
        "O" => "#f38ba8",
        "N" => "#89b4fa",
        "S" => "#f9e2af",
        "Cl" or "F" or "Br" => "#a6e3a1",
        "Fe" or "Cu" or "Na" => "#fab387",
        _ => isDarkMode ? "#89dceb" : "#0284c7"
    };

    private static string EscapeXml(string str) => str
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
