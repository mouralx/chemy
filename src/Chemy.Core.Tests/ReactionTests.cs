using System.Collections.Generic;
using Chemy.Core;
using Chemy.Core.Reactions;
using Xunit;

namespace Chemy.Core.Tests;

public class ReactionTests
{
    [Theory]
    [InlineData("H2 + O2 -> H2O", "2H2 + O2 -> 2H2O")]
    [InlineData("Fe + O2 -> Fe2O3", "4Fe + 3O2 -> 2Fe2O3")]
    [InlineData("C6H12O6 + O2 -> CO2 + H2O", "C6H12O6 + 6O2 -> 6CO2 + 6H2O")]
    [InlineData("CH4 + O2 -> CO2 + H2O", "CH4 + 2O2 -> CO2 + 2H2O")]
    [InlineData("CuSO4 + NaOH -> Cu(OH)2 + Na2SO4", "CuSO4 + 2NaOH -> Cu(OH)2 + Na2SO4")]
    [InlineData("Al + HCl -> AlCl3 + H2", "2Al + 6HCl -> 2AlCl3 + 3H2")]
    public void Balance_UnbalancedReaction_BalancesCorrectly(string unbalanced, string expectedBalanced)
    {
        var reaction = Reaction.Parse(unbalanced);
        Assert.False(reaction.IsBalanced);

        var balanced = reaction.Balance();
        Assert.True(balanced.IsBalanced);
        Assert.Equal(expectedBalanced, balanced.ToString());
    }

    [Fact]
    public void CalculateProductYield_ValidInputs_ReturnsCorrectMass()
    {
        // 2H2 + O2 -> 2H2O
        // 4.032g H2 -> ~36.03g H2O
        var reaction = Reaction.Parse("H2 + O2 -> H2O");
        var yieldResult = Stoichiometry.CalculateProductYield(reaction, "H2", 4.032, "H2O");

        Assert.Equal("H2", yieldResult.Reactant.ChemicalFormula);
        Assert.Equal("H2O", yieldResult.Product.ChemicalFormula);
        Assert.InRange(yieldResult.ProductMassGrams, 35.5, 36.5);
    }

    [Fact]
    public void CalculateLimitingReactant_IdentifiesLimitingReactantAndYields()
    {
        // 2H2 + O2 -> 2H2O
        // If we have 2.0g H2 (~1 mol H2 -> needs 0.5 mol O2 = 16g O2)
        // Given 2.0g H2 and 100.0g O2, H2 is limiting!
        var reaction = Reaction.Parse("H2 + O2 -> H2O");
        var masses = new Dictionary<string, double>
        {
            { "H2", 2.0 },
            { "O2", 100.0 }
        };

        var result = Stoichiometry.CalculateLimitingReactant(reaction, masses);

        Assert.Equal("H2", result.LimitingReactant.Molecule.ChemicalFormula);
        Assert.True(result.ProductYieldsGrams.ContainsKey("H2O"));
        Assert.InRange(result.ProductYieldsGrams["H2O"], 17.5, 18.5);
    }
}
