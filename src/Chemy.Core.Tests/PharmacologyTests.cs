using Chemy.Core;
using Chemy.Core.Pharmacology;
using Xunit;

namespace Chemy.Core.Tests;

public class PharmacologyTests
{
    [Fact]
    public void Analyze_Aspirin_ComputesValidAdmetProfile()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var admet = AdmetEngine.Analyze(aspirin);

        Assert.NotNull(admet);
        Assert.True(admet.MolecularWeight > 170.0 && admet.MolecularWeight < 190.0);
        Assert.True(admet.TpsaAngstrom2 > 0);
        Assert.True(admet.QedDrugLikenessScore > 0.4);
        Assert.True(admet.PassesLipinskiRuleOf5);
    }
}
