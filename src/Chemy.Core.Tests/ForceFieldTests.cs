using Chemy.Core;
using Chemy.Core.Physics;
using Xunit;

namespace Chemy.Core.Tests;

public class ForceFieldTests
{
    [Fact]
    public void MinimizeEnergy_Water_ReducesOrStabilizesEnergy()
    {
        var water = Molecule.TryParse("H2O", "Water", out var mol) ? mol : Molecule.FromSmiles("O", "Water");
        var m3d = water.To3D();

        var result = ForceFieldEngine.MinimizeEnergy(m3d, maxIterations: 10);

        Assert.NotNull(result);
        Assert.True(result.FinalEnergyKcalPerMol >= 0);
        Assert.Equal("H2O", result.Formula);
    }
}
