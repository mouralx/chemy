using Chemy.Core;
using Xunit;

namespace Chemy.Core.Tests;

public class ExplanationTests
{
    [Fact]
    public void BalanceWithSteps_ProducesFiveStructuredSteps()
    {
        var reaction = Reaction.Parse("CH4 + O2 -> CO2 + H2O");
        var result = reaction.BalanceWithSteps();

        Assert.NotNull(result);
        Assert.True(result.BalancedReaction.IsBalanced);
        Assert.Equal(5, result.Steps.Count);

        Assert.Equal("Initial Atom Count Audit", result.Steps[0].Title);
        Assert.Equal("Setting Up Conservation Equations", result.Steps[1].Title);
        Assert.Equal("Matrix Representation & Gaussian Elimination", result.Steps[2].Title);
        Assert.Equal("Clearing Fractions & Integer Scaling", result.Steps[3].Title);
        Assert.Equal("Final Balance Verification", result.Steps[4].Title);

        string explanationText = result.FormattedExplanation;
        Assert.Contains("# Step-by-Step Reaction Balancing", explanationText);
        Assert.Contains("CH4 + 2O2 -> CO2 + 2H2O", explanationText);
    }
}
