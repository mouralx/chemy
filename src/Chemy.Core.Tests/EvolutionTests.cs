using Chemy.Core.Evolution;
using Xunit;

namespace Chemy.Core.Tests;

public class EvolutionTests
{
    [Fact]
    public void EvolveLeadCandidate_ReturnsFiveEvolvedDerivatives()
    {
        var result = MolecularEvolverEngine.EvolveLeadCandidate("CC(=O)Oc1ccccc1C(=O)O", generations: 20);

        Assert.NotNull(result);
        Assert.Equal(5, result.Candidates.Count);
        Assert.All(result.Candidates, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.CandidateName));
            Assert.False(string.IsNullOrWhiteSpace(c.Rationale));
            Assert.True(c.QedScore > 0);
        });
    }
}
