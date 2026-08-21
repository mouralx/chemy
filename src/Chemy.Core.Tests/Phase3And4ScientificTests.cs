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
        // Matrix:
        // C balance: [1, 0, -1, -1]
        // O balance: [0, 2, -1, -2]
        int[,] matrix = new int[,]
        {
            { 1, 0, -1, -1 },
            { 0, 2, -1, -2 }
        };

        var basis = MatrixSolver.SolveNullspaceBasis(matrix);

        // System has 4 columns and rank 2 -> nullspace dimension is 2
        Assert.Equal(2, basis.Count);
    }

    [Fact]
    public void ShomateThermodynamics_WaterGas_CalculatesStandardEnthalpyAndEntropy()
    {
        var result298 = ShomateThermodynamics.Evaluate("H2O(g)", 298.15);
        Assert.NotNull(result298);

        // NIST standard value for H2O(g) formation enthalpy at 298.15 K is -241.83 kJ/mol
        Assert.InRange(result298.StandardEnthalpyH, -245.0, -238.0);

        // NIST standard entropy S° at 298.15 K is 188.8 J/(mol*K)
        Assert.InRange(result298.StandardEntropyS, 180.0, 200.0);

        // High temperature test at 1000 K
        var result1000 = ShomateThermodynamics.Evaluate("H2O(g)", 1000.0);
        Assert.NotNull(result1000);
        Assert.True(result1000.HeatCapacityCp > result298.HeatCapacityCp); // Cp increases with T
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
