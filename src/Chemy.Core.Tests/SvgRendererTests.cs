using System.IO;
using Chemy.Core;
using Xunit;

namespace Chemy.Core.Tests;

public class SvgRendererTests
{
    [Fact]
    public void MoleculeToSvg_GeneratesValidSvgMarkup()
    {
        var mol = Molecule.Parse("C6H12O6", "Glucose");
        string svg = mol.ToSvg();

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg.Trim());
        Assert.EndsWith("</svg>", svg.Trim());
        Assert.Contains("Glucose", svg);
        Assert.Contains("C6H12O6", svg);
    }

    [Fact]
    public void ReactionToSvg_GeneratesValidSvgMarkup()
    {
        var reaction = Reaction.Parse("CH4 + O2 -> CO2 + H2O");
        string svg = reaction.ToSvg();

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg.Trim());
        Assert.EndsWith("</svg>", svg.Trim());
        Assert.Contains("CH4 + 2O2 -&gt; CO2 + 2H2O", svg);
    }

    [Fact]
    public void SaveSvg_SavesMoleculeAndReactionSvgToDisk()
    {
        string tempMolFile = Path.Combine(Path.GetTempPath(), "test_mol.svg");
        string tempRxnFile = Path.Combine(Path.GetTempPath(), "test_rxn.svg");

        try
        {
            var mol = Molecule.Parse("H2O");
            mol.SaveSvg(tempMolFile);
            Assert.True(File.Exists(tempMolFile));
            Assert.Contains("<svg", File.ReadAllText(tempMolFile));

            var rxn = Reaction.Parse("H2 + O2 -> H2O");
            rxn.SaveSvg(tempRxnFile);
            Assert.True(File.Exists(tempRxnFile));
            Assert.Contains("<svg", File.ReadAllText(tempRxnFile));
        }
        finally
        {
            if (File.Exists(tempMolFile)) File.Delete(tempMolFile);
            if (File.Exists(tempRxnFile)) File.Delete(tempRxnFile);
        }
    }
}
