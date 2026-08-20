using Chemy.Core;
using Chemy.Core.Environmental;
using Chemy.Core.Evolution;
using Chemy.Core.Spectroscopy;
using Chemy.Core.Thermodynamics;
using Xunit;

namespace Chemy.Core.Tests;

public class UniversalEngineTests
{
    [Theory]
    [InlineData("CC(=O)OCC")] // Ethyl acetate (Ester)
    [InlineData("CC(=O)C")]   // Acetone (Ketone)
    [InlineData("CCN(C)C")]   // Triethylamine (Amine)
    [InlineData("c1ccccc1")]  // Benzene (Aromatic)
    [InlineData("C6H12O6")]   // Glucose (Polyol)
    public void MolecularEvolver_ArbitraryMolecules_GeneratesFiveValidCandidates(string input)
    {
        var result = MolecularEvolverEngine.EvolveLeadCandidate(input, generations: 30);

        Assert.NotNull(result);
        Assert.Equal(5, result.Candidates.Count);
        Assert.All(result.Candidates, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.CandidateName));
            Assert.False(string.IsNullOrWhiteSpace(c.Rationale));
            Assert.False(string.IsNullOrWhiteSpace(c.ToxicityImprovement));
            Assert.True(c.QedScore > 0.0);
        });
    }

    [Theory]
    [InlineData("C6H4Cl2")]      // Dichlorobenzene (Halogenated organopollutant)
    [InlineData("C10H19O6PS2")]  // Malathion (Organophosphate pesticide)
    [InlineData("C10H8O4")]      // PET monomer (Polyester plastic)
    [InlineData("C8HF15O2")]     // PFOA (PFAS forever chemical)
    public void EcoCleanEngine_ArbitraryPollutants_GeneratesTailoredCascade(string pollutant)
    {
        var cascade = EcoCleanEngine.SolveDegradationCascade(pollutant);

        Assert.NotNull(cascade);
        Assert.NotEmpty(cascade.DegradationCascade);
        Assert.True(cascade.TotalMineralizationEfficiencyPercent >= 90.0);
        Assert.False(string.IsNullOrWhiteSpace(cascade.MineralizedEndProducts));
    }

    [Theory]
    [InlineData("CC=O")]       // Acetaldehyde (Aldehyde)
    [InlineData("C=CC")]       // Propene (Alkene)
    [InlineData("C#CC")]       // Propyne (Alkyne)
    [InlineData("CC(=O)NC")]   // N-Methylacetamide (Amide)
    public void SpectroscopyEngine_AllFunctionalGroups_PredictsValidSpectra(string formula)
    {
        var molecule = Molecule.FromSmiles(formula, "Test");
        var spec = SpectroscopyEngine.Predict(molecule);

        Assert.NotNull(spec);
        Assert.NotEmpty(spec.H1NmrPeaks);
        Assert.NotEmpty(spec.C13NmrPeaks);
        Assert.NotEmpty(spec.IrBands);
    }

    [Fact]
    public void ThermodynamicsEngine_ArbitraryUnknownReaction_CalculatesViaBensonAdditivity()
    {
        // Reaction with custom complex molecules not in standard tables
        var reaction = Reaction.Parse("C4H10 + O2 -> CO2 + H2O");
        var thermo = reaction.GetThermodynamics(298.15);

        Assert.NotNull(thermo);
        Assert.True(thermo.IsExothermic);
        Assert.True(thermo.IsSpontaneous);
        Assert.True(thermo.EnthalpyChangekJ < 0.0);
    }
}
