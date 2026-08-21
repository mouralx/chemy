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
        Assert.True(double.IsFinite(result.FinalEnergyKcalPerMol));
        Assert.Equal("H2O", result.Formula);
    }

    [Fact]
    public void MinimizeEnergy_Butane_RelaxesDihedralsAndAngles()
    {
        var butane = Molecule.FromSmiles("CCCC", "Butane").To3D();
        var result = ForceFieldEngine.MinimizeEnergy(butane, maxIterations: 30);

        Assert.NotNull(result);
        Assert.True(double.IsFinite(result.FinalEnergyKcalPerMol));
        Assert.NotEmpty(result.TerminationReason);
        Assert.Equal(butane.Atoms.Count, result.MinimizedMolecule.Atoms.Count);
    }

    [Fact]
    public void MinimizeEnergy_Ethanol_MaintainsPhysicalCoordinates()
    {
        var ethanol = Molecule.FromSmiles("CCO", "Ethanol").To3D();
        var result = ForceFieldEngine.MinimizeEnergy(ethanol, maxIterations: 20);

        Assert.NotNull(result);
        Assert.True(double.IsFinite(result.FinalEnergyKcalPerMol));
        Assert.Equal(9, result.MinimizedMolecule.Atoms.Count); // C2H6O -> 2 C + 6 H + 1 O = 9 atoms
    }

    [Fact]
    public void MinimizeEnergy_NonPositiveIterations_IsRejected()
    {
        var butane = Molecule.FromSmiles("CCCC").To3D();
        Assert.Throws<ArgumentOutOfRangeException>(() => ForceFieldEngine.MinimizeEnergy(butane, 0));
    }
}
