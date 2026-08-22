using Chemy.Core;
using Chemy.Core.Physics;
using Chemy.Core.Spatial;
using Xunit;

namespace Chemy.Core.Tests;

public class ForceFieldTests
{
    [Fact]
    public void MinimizeEnergy_Water_ReducesOrStabilizesEnergy()
    {
        var water = Molecule.FromSmiles("O", "Water");
        var m3d = water.To3D();

        var result = ForceFieldEngine.MinimizeEnergy(m3d, maxIterations: 10);

        Assert.NotNull(result);
        Assert.True(result.FinalEnergyKcalPerMol <= result.InitialEnergyKcalPerMol);
        Assert.Equal("H2O", result.Formula);
    }

    [Fact]
    public void MinimizeEnergy_Butane_RelaxesDihedralsAndAngles()
    {
        var butane = Molecule.FromSmiles("CCCC", "Butane").To3D();
        var result = ForceFieldEngine.MinimizeEnergy(butane, maxIterations: 30);

        Assert.NotNull(result);
        Assert.True(result.FinalEnergyKcalPerMol <= result.InitialEnergyKcalPerMol);
        Assert.Equal(butane.Atoms.Count, result.MinimizedMolecule.Atoms.Count);
    }

    [Fact]
    public void MinimizeEnergy_Ethanol_MaintainsPhysicalCoordinates()
    {
        var ethanol = Molecule.FromSmiles("CCO", "Ethanol").To3D();
        var result = ForceFieldEngine.MinimizeEnergy(ethanol, maxIterations: 20);

        Assert.NotNull(result);
        Assert.True(result.FinalEnergyKcalPerMol >= 0);
        Assert.Equal(9, result.MinimizedMolecule.Atoms.Count); // C2H6O -> 2 C + 6 H + 1 O = 9 atoms
    }

    [Fact]
    public void MinimizeEnergy_InvalidOptimizationControls_FailClosed()
    {
        var water = Molecule.FromSmiles("O", "Water").To3D();

        Assert.Throws<ArgumentOutOfRangeException>(() => ForceFieldEngine.MinimizeEnergy(water, maxIterations: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ForceFieldEngine.MinimizeEnergy(water, gradientTolerance: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ForceFieldEngine.MinimizeEnergy(water, gradientTolerance: double.NaN));
    }

    [Fact]
    public void CalculateEnergyComponents_SumMatchesTotalEnergy()
    {
        var butane = Molecule.FromSmiles("CCCC", "Butane").To3D();

        var components = ForceFieldEngine.CalculateEnergyComponents(butane);
        double total = ForceFieldEngine.CalculateTotalEnergy(butane);

        Assert.Equal(total, components.TotalKcalPerMol, precision: 10);
        Assert.True(double.IsFinite(components.BondStretchKcalPerMol));
        Assert.True(double.IsFinite(components.AngleBendKcalPerMol));
        Assert.True(double.IsFinite(components.TorsionKcalPerMol));
        Assert.True(double.IsFinite(components.InversionKcalPerMol));
        Assert.True(double.IsFinite(components.VanDerWaalsKcalPerMol));
    }

    [Fact]
    public void CalculateEnergyComponents_CarbonylInversionUsesPublishedSpecialForceConstant()
    {
        var formaldehyde = Molecule.FromSmiles("C=O", "Formaldehyde");
        var methanimine = Molecule.FromSmiles("C=N", "Methanimine");
        Assert.Equal(4, formaldehyde.Atoms.Count);
        Assert.Equal(5, methanimine.Atoms.Count);

        Vector3D[] sharedPositions =
        [
            new(0.0, 0.0, 0.0),
            new(1.20, 0.0, 0.35),
            new(-0.60, 1.00, 0.0),
            new(-0.60, -1.00, 0.0)
        ];

        var carbonyl3D = new Molecule3D(
            formaldehyde.Name,
            formaldehyde.ChemicalFormula,
            "OutOfPlaneRegression",
            120.0,
            formaldehyde.Atoms.Select((atom, index) => new Atom3D(atom, sharedPositions[index])).ToArray(),
            formaldehyde);

        var iminePositions = sharedPositions.Append(new Vector3D(1.80, 0.70, 0.35)).ToArray();
        var imine3D = new Molecule3D(
            methanimine.Name,
            methanimine.ChemicalFormula,
            "OutOfPlaneRegression",
            120.0,
            methanimine.Atoms.Select((atom, index) => new Atom3D(atom, iminePositions[index])).ToArray(),
            methanimine);

        double carbonylInversion = ForceFieldEngine.CalculateEnergyComponents(carbonyl3D).InversionKcalPerMol;
        double imineInversion = ForceFieldEngine.CalculateEnergyComponents(imine3D).InversionKcalPerMol;

        Assert.True(carbonylInversion > 0.0);
        Assert.True(imineInversion > 0.0);
        Assert.Equal(50.0 / 6.0, carbonylInversion / imineInversion, precision: 8);
    }
}
