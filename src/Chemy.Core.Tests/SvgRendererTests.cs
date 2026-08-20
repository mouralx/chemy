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

    [Fact]
    public void MoleculeToSkeletalSvg_Ibuprofen_RendersChemDrawLinesAndHeteroatoms()
    {
        var ibuprofen = Molecule.FromSmiles("CC(C)Cc1ccc(cc1)C(C)C(=O)O", "Ibuprofen");
        string svg = ibuprofen.ToSkeletalSvg(isDarkMode: true);

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg.Trim());
        Assert.EndsWith("</svg>", svg.Trim());
        Assert.Contains("<line", svg);
        Assert.Contains("OH", svg); // Hydroxyl group
        Assert.Contains("O", svg);  // Carbonyl oxygen
    }

    [Fact]
    public void MoleculeToSkeletalSvg_Aspirin_RendersAromaticAndEsterBonds()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        string svg = aspirin.ToSkeletalSvg(isDarkMode: false);

        Assert.NotNull(svg);
        Assert.Contains("<line", svg);
        Assert.Contains("OH", svg);
    }

    [Fact]
    public void MoleculeToSkeletalSvg_Caffeine_RendersFusedRingsAndCarbonyls()
    {
        var caffeine = Molecule.FromSmiles("CN1C=NC2=C1C(=O)N(C(=O)N2C)C", "Caffeine");
        string svg = caffeine.ToSkeletalSvg(isDarkMode: true);

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg.Trim());
        Assert.EndsWith("</svg>", svg.Trim());
        Assert.Contains("<line", svg);
        Assert.Contains("O", svg);
        Assert.Contains("N", svg);
    }

    [Theory]
    [InlineData("H2O", "Water")]
    [InlineData("CH4", "Methane")]
    [InlineData("CO2", "Carbon Dioxide")]
    [InlineData("CC(=O)Oc1ccccc1C(=O)O", "Aspirin")]
    [InlineData("CN1C=NC2=C1C(=O)N(C(=O)N2C)C", "Caffeine")]
    [InlineData("CC(=O)Nc1ccc(O)cc1", "Paracetamol")]
    [InlineData("CC(C)Cc1ccc(cc1)C(C)C(=O)O", "Ibuprofen")]
    [InlineData("CCO", "Ethanol")]
    [InlineData("CC(=O)C", "Acetone")]
    [InlineData("c1ccccc1", "Benzene")]
    [InlineData("ATP", "Adenosine Triphosphate")]
    [InlineData("C10H16N5O13P3", "Adenosine Triphosphate")]
    [InlineData("Glucose", "D-Glucose")]
    [InlineData("C6H12O6", "D-Glucose")]
    [InlineData("PFOA", "Perfluorooctanoic Acid")]
    [InlineData("C8HF15O2", "Perfluorooctanoic Acid")]
    public void MoleculeToSkeletalSvg_CatalogCompounds_AllRenderValidSvgWithBonds(string input, string name)
    {
        Molecule molecule;
        if (Chemy.Core.Structure.CompoundRegistry.TryResolve(input, out var regName, out var regSmiles))
        {
            molecule = Molecule.FromSmiles(regSmiles, regName);
        }
        else if (input.Contains('(') || input.Contains('=') || input.Contains('c') || input.Contains('N'))
        {
            molecule = Molecule.FromSmiles(input, name) ?? Molecule.Parse(input, name);
        }
        else
        {
            molecule = Molecule.TryParse(input, name, out var m) ? m : Molecule.FromSmiles(input, name);
        }

        string svg = molecule.ToSkeletalSvg(isDarkMode: true);

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg.Trim());
        Assert.EndsWith("</svg>", svg.Trim());
        if (molecule.Atoms.Count(a => a.Element.Symbol != "H") > 1)
        {
            Assert.Contains("<line", svg);
        }
    }
}
