using Chemy.Core;
using Chemy.Core.Thermodynamics;
using Xunit;

namespace Chemy.Core.Tests;

public class ThermodynamicsTests
{
    [Fact]
    public void GetThermodynamics_MethaneCombustion_CalculatesCorrectExothermicProperties()
    {
        // CH4 + 2O2 -> CO2 + 2H2O
        // ΔH = -393.5 + 2(-285.8) - (-74.6 + 0) = -393.5 - 571.6 + 74.6 = -890.5 kJ/mol
        var reaction = Reaction.Parse("CH4 + O2 -> CO2 + H2O");
        var thermo = reaction.GetThermodynamics(298.15);

        Assert.True(thermo.IsExothermic);
        Assert.False(thermo.IsEndothermic);
        Assert.True(thermo.IsSpontaneous);
        Assert.InRange(thermo.EnthalpyChangekJ, -895.0, -885.0);
        Assert.InRange(thermo.GibbsFreeEnergykJ, -820.0, -800.0);
    }

    [Fact]
    public void GetThermodynamics_IronRusting_CalculatesSpontaneousExothermicReaction()
    {
        // 4Fe + 3O2 -> 2Fe2O3
        var reaction = Reaction.Parse("Fe + O2 -> Fe2O3");
        var thermo = reaction.GetThermodynamics();

        Assert.True(thermo.IsExothermic);
        Assert.True(thermo.IsSpontaneous);
        Assert.InRange(thermo.EnthalpyChangekJ, -1650.0, -1640.0);
    }

    [Fact]
    public void GetThermodynamics_BensonAdditivity_EstimatesOrganicEnthalpy()
    {
        // Test arbitrary organic compound via Benson group additivity combustion: 2C4H10 + 13O2 -> 8CO2 + 10H2O
        var isobutane = Molecule.FromSmiles("CC(C)C", "Isobutane");
        var reaction = new Reaction(
            [new ReactionComponent(isobutane, 2), new ReactionComponent(Molecule.Parse("O2", "Oxygen"), 13)],
            [new ReactionComponent(Molecule.Parse("CO2", "CarbonDioxide"), 8), new ReactionComponent(Molecule.Parse("H2O", "Water"), 10)]
        );

        var thermo = ThermodynamicsEngine.GetThermodynamics(reaction);
        Assert.NotNull(thermo);
        Assert.True(thermo.IsExothermic);
        Assert.True(thermo.IsSpontaneous);
    }
}
