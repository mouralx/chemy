namespace Chemy.Core.Tests;

using Chemy.Core;
using Chemy.Core.Graph;
using Chemy.Core.Reactions;
using Chemy.Core.Structure;
using Chemy.Core.Thermodynamics;
using Xunit;

public class Phase3And4ScientificTests
{
    [Fact]
    public void CycleBasis_Benzene_ReturnsSingleSixMemberedRing()
    {
        var benzene = SmilesParser.Parse("c1ccccc1");
        var sssr = CycleBasis.ComputeSssr(benzene);

        Assert.Equal(1, sssr.FrerejacqueNumber);
        Assert.Single(sssr.Rings);
        Assert.Equal(6, sssr.Rings[0].Count);
    }

    [Fact]
    public void CycleBasis_Naphthalene_ReturnsTwoIndependentSixMemberedRings()
    {
        // Naphthalene (fused bicyclic aromatic ring)
        var naphthalene = SmilesParser.Parse("c1ccc2ccccc2c1");
        var sssr = CycleBasis.ComputeSssr(naphthalene);

        Assert.Equal(2, sssr.FrerejacqueNumber);
        Assert.Equal(2, sssr.Rings.Count);
        Assert.All(sssr.Rings, r => Assert.Equal(6, r.Count));
    }

    [Fact]
    public void MatrixSolver_UnderdeterminedCarbonCombustion_ReturnsNullspaceBasisOfDimensionTwo()
    {
        // Reaction: C + O2 -> CO + CO2
        int[,] matrix = new int[,]
        {
            { 1, 0, -1, -1 },
            { 0, 2, -1, -2 }
        };

        var basis = MatrixSolver.SolveNullspaceBasis(matrix);

        Assert.Equal(2, basis.Count);
    }

    [Fact]
    public void Reaction_BalanceIndependentPathways_DecomposesUnderdeterminedReaction()
    {
        var rxn = Reaction.Parse("C + O2 -> CO + CO2");
        var pathways = rxn.BalanceIndependentPathways();

        Assert.True(pathways.Count >= 2);
    }

    [Fact]
    public void SmilesParser_UnsupportedStereochemistry_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => SmilesParser.Parse("C@C"));
        Assert.Throws<NotSupportedException>(() => SmilesParser.Parse("F[C@](Cl)(Br)I"));
        Assert.Throws<NotSupportedException>(() => SmilesParser.Parse("C/C=C/C"));
    }

    [Fact]
    public void ShomateThermodynamics_WaterGas_UsesPublishedIntervalWithoutExtrapolation()
    {
        var result500 = ShomateThermodynamics.Evaluate("H2O(g)", 500.0);
        var result1000 = ShomateThermodynamics.Evaluate("H2O(g)", 1000.0);
        Assert.NotNull(result500);
        Assert.NotNull(result1000);

        Assert.InRange(result500.StandardEnthalpyH, -234.91, -234.89);
        Assert.InRange(result500.StandardEntropyS, 206.52, 206.54);
        Assert.InRange(result500.HeatCapacityCp, 35.21, 35.23);
        Assert.Equal(new ShomateTemperatureRange(500.0, 1700.0), result500.CoefficientRange);
        Assert.StartsWith("https://webbook.nist.gov/", result500.SourceUrl, StringComparison.Ordinal);
        Assert.True(result1000.HeatCapacityCp > result500.HeatCapacityCp);
    }

    [Fact]
    public void ShomateThermodynamics_OutOfRangeTemperature_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShomateThermodynamics.Evaluate("H2O(g)", 499.99));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShomateThermodynamics.Evaluate("H2O(g)", 6000.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShomateThermodynamics.Evaluate("CO2(g)", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShomateThermodynamics.Evaluate("CO2(g)", double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShomateThermodynamics.Evaluate("CO2(g)", 0.0));
    }

    [Fact]
    public void ShomateThermodynamics_IntervalBoundary_SelectsUpperSegmentDeterministically()
    {
        var belowBoundary = ShomateThermodynamics.Evaluate("CO2(g)", 1199.999);
        var atBoundary = ShomateThermodynamics.Evaluate("CO2(g)", 1200.0);

        Assert.NotNull(belowBoundary);
        Assert.NotNull(atBoundary);
        Assert.Equal(new ShomateTemperatureRange(298.0, 1200.0), belowBoundary.CoefficientRange);
        Assert.Equal(new ShomateTemperatureRange(1200.0, 6000.0), atBoundary.CoefficientRange);
        Assert.InRange(Math.Abs(belowBoundary.HeatCapacityCp - atBoundary.HeatCapacityCp), 0.0, 0.05);
    }

    [Fact]
    public void ShomateThermodynamics_SupportedRanges_AreSpeciesSpecific()
    {
        Assert.Equal(
            [new ShomateTemperatureRange(500.0, 1700.0), new ShomateTemperatureRange(1700.0, 6000.0)],
            ShomateThermodynamics.GetSupportedTemperatureRanges("H2O(g)"));
        Assert.Equal(3, ShomateThermodynamics.GetSupportedTemperatureRanges("O2(g)").Count);
        Assert.Empty(ShomateThermodynamics.GetSupportedTemperatureRanges("unsupported"));
        Assert.Null(ShomateThermodynamics.Evaluate("unsupported", 500.0));
    }

    [Fact]
    public void ShomateThermodynamics_CarbonDioxide_MatchesJANAFEnthalpy()
    {
        var result298 = ShomateThermodynamics.Evaluate("CO2(g)", 298.15);
        Assert.NotNull(result298);

        // NIST standard Delta H_f° for CO2(g) is -393.5 kJ/mol
        Assert.InRange(result298.StandardEnthalpyH, -395.0, -390.0);
    }
}
