using Xunit;

namespace Chemy.Core.Tests;

public class FormulaParserTests
{
    [Theory]
    [InlineData("H2O", 2, 0, 1, 18.015)]
    [InlineData("C6H12O6", 12, 6, 6, 180.156)]
    [InlineData("NaCl", 0, 0, 0, 58.44)]
    public void Parse_SimpleFormulas_ReturnsExpectedMolecule(string formula, int expectedH, int expectedC, int expectedO, double minWeight)
    {
        var mol = Molecule.Parse(formula);

        Assert.NotNull(mol);
        Assert.Equal(formula, mol.Name);
        Assert.True(mol.MolecularWeight >= minWeight - 1.0 && mol.MolecularWeight <= minWeight + 1.0);

        Assert.Equal(expectedH, mol.Atoms.Count(a => a.Element.Symbol == "H"));
        Assert.Equal(expectedC, mol.Atoms.Count(a => a.Element.Symbol == "C"));
        Assert.Equal(expectedO, mol.Atoms.Count(a => a.Element.Symbol == "O"));
    }

    [Fact]
    public void Parse_EmpiricalFormula_DoesNotInventCovalentTopology()
    {
        var glucose = Molecule.Parse("C6H12O6");
        Assert.Empty(glucose.Bonds);
        Assert.Equal(0, glucose.NetCharge);
    }

    [Fact]
    public void Parse_NestedBrackets_CalculatesCorrectCounts()
    {
        var caoh2 = Molecule.Parse("Ca(OH)2", "Calcium Hydroxide");
        Assert.Equal("Calcium Hydroxide", caoh2.Name);
        Assert.Equal(1, caoh2.Atoms.Count(a => a.Element.Symbol == "Ca"));
        Assert.Equal(2, caoh2.Atoms.Count(a => a.Element.Symbol == "O"));
        Assert.Equal(2, caoh2.Atoms.Count(a => a.Element.Symbol == "H"));

        var fe2so43 = Molecule.Parse("Fe2(SO4)3");
        Assert.Equal(2, fe2so43.Atoms.Count(a => a.Element.Symbol == "Fe"));
        Assert.Equal(3, fe2so43.Atoms.Count(a => a.Element.Symbol == "S"));
        Assert.Equal(12, fe2so43.Atoms.Count(a => a.Element.Symbol == "O"));

        var complex = Molecule.Parse("[Cu(NH3)4]SO4");
        Assert.Equal(1, complex.Atoms.Count(a => a.Element.Symbol == "Cu"));
        Assert.Equal(4, complex.Atoms.Count(a => a.Element.Symbol == "N"));
        Assert.Equal(12, complex.Atoms.Count(a => a.Element.Symbol == "H"));
        Assert.Equal(1, complex.Atoms.Count(a => a.Element.Symbol == "S"));
        Assert.Equal(4, complex.Atoms.Count(a => a.Element.Symbol == "O"));
    }

    [Fact]
    public void Parse_Hydrate_ParsesDotAndStarNotations()
    {
        var cuso45h2o = Molecule.Parse("CuSO4*5H2O");
        Assert.Equal(1, cuso45h2o.Atoms.Count(a => a.Element.Symbol == "Cu"));
        Assert.Equal(1, cuso45h2o.Atoms.Count(a => a.Element.Symbol == "S"));
        Assert.Equal(9, cuso45h2o.Atoms.Count(a => a.Element.Symbol == "O"));
        Assert.Equal(10, cuso45h2o.Atoms.Count(a => a.Element.Symbol == "H"));

        var dotNotation = Molecule.Parse("CuSO4.5H2O");
        Assert.Equal(9, dotNotation.Atoms.Count(a => a.Element.Symbol == "O"));
    }

    [Theory]
    [InlineData("SO4^2-", -2)]
    [InlineData("NH4+", 1)]
    [InlineData("Fe^3+", 3)]
    [InlineData("OH-", -1)]
    public void Parse_Ions_SetsCorrectNetCharge(string formula, int expectedCharge)
    {
        var ion = Molecule.Parse(formula);
        Assert.Equal(expectedCharge, ion.NetCharge);
    }

    [Theory]
    [InlineData("H2O(extra")]
    [InlineData("Ca(OH")]
    [InlineData("Xx2O")]
    [InlineData("123")]
    [InlineData("")]
    public void Parse_InvalidFormulas_ThrowsException(string invalidFormula)
    {
        Assert.ThrowsAny<Exception>(() => Molecule.Parse(invalidFormula));
        Assert.False(Molecule.TryParse(invalidFormula, out _));
    }
}
