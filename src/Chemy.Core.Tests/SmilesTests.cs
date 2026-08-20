using System.Linq;
using Chemy.Core;
using Chemy.Core.Structure;
using Xunit;

namespace Chemy.Core.Tests;

public class SmilesTests
{
    [Theory]
    [InlineData("CCO", 2, 6, 1, "Ethanol")]
    [InlineData("CCOC", 3, 8, 1, "Ethyl Methyl Ether")]
    [InlineData("CCOCC", 4, 10, 1, "Diethyl Ether")]
    [InlineData("CCCC", 4, 10, 0, "Butane")]
    [InlineData("CC(=O)O", 2, 4, 2, "Acetic Acid")]
    [InlineData("CC(C)C", 4, 10, 0, "Isobutane")]
    public void FromSmiles_ParsesOrganicMoleculesCorrectly(string smiles, int expectedC, int expectedH, int expectedO, string name)
    {
        var mol = Molecule.FromSmiles(smiles, name);

        Assert.NotNull(mol);
        Assert.Equal(name, mol.Name);
        Assert.Equal(expectedC, mol.Atoms.Count(a => a.Element.Symbol == "C"));
        Assert.Equal(expectedH, mol.Atoms.Count(a => a.Element.Symbol == "H"));
        Assert.Equal(expectedO, mol.Atoms.Count(a => a.Element.Symbol == "O"));
    }

    [Fact]
    public void FromSmiles_HandlesDisconnectedDotNotation()
    {
        var salt = Molecule.FromSmiles("[Na+].[Cl-]", "Sodium Chloride");
        Assert.NotNull(salt);
        Assert.Equal(2, salt.Atoms.Count);
        Assert.Contains(salt.Atoms, a => a.Element.Symbol == "Na" && a.NetCharge == 1);
        Assert.Contains(salt.Atoms, a => a.Element.Symbol == "Cl" && a.NetCharge == -1);
    }

    [Fact]
    public void GetFunctionalGroups_DetectsAlcoholAndCarboxylicAcid()
    {
        var ethanol = Molecule.FromSmiles("CCO");
        var ethanolFgs = ethanol.GetFunctionalGroups();
        Assert.Contains(FunctionalGroup.Alcohol, ethanolFgs);

        var aceticAcid = Molecule.FromSmiles("CC(=O)O");
        var acidFgs = aceticAcid.GetFunctionalGroups();
        Assert.Contains(FunctionalGroup.CarboxylicAcid, acidFgs);
    }
}
