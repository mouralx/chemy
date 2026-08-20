using Chemy.Core.Electrochemistry;
using Xunit;

namespace Chemy.Core.Tests;

public class ElectrochemistryTests
{
    [Fact]
    public void CalculateNernstPotential_StandardConditions_ReturnsStandardPotential()
    {
        // Q = 1 -> ln(Q) = 0 -> E_cell = E°_cell
        var nernst = ElectrochemistryEngine.CalculateNernstPotential(
            standardCellPotentialVolts: 1.10,
            electronsTransferred: 2,
            reactionQuotientQ: 1.0,
            temperatureKelvin: 298.15
        );

        Assert.Equal(1.10, nernst.CellPotentialVolts, precision: 2);
        Assert.True(nernst.IsSpontaneousGalvanic);
    }
}
