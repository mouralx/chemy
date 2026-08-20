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
            Assert.False(string.IsNullOrWhiteSpace(c.Smiles));
            Assert.True(c.QedScore > 0);
            Assert.True(c.MolecularWeight > 0);
        });

        // Verify tetrazole bioisostere candidate is generated
        Assert.Contains(result.Candidates, c => c.CandidateName.Contains("Tetrazole") || c.Smiles.Contains("nnn"));
    }
}
