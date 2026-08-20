using Chemy.Core;
using Chemy.Core.Spatial;
using Xunit;

namespace Chemy.Core.Tests;

public class Geometry3DTests
{
    [Fact]
    public void Generate3D_Water_CalculatesBentGeometryAndFormatsXyzPdb()
    {
        var water = Molecule.Parse("H2O");
        var m3d = Geometry3DEngine.Generate3D(water);

        Assert.NotNull(m3d);
        Assert.Equal("Bent", m3d.VseprShape);
        Assert.Equal(104.5, m3d.IdealBondAngleDegrees);
        Assert.Equal(3, m3d.Atoms.Count);

        string xyz = m3d.ToXyz();
        Assert.Contains("3", xyz);
        Assert.Contains("Bent", xyz);

        string pdb = m3d.ToPdb();
        Assert.Contains("HEADER", pdb);
        Assert.Contains("HETATM", pdb);
        Assert.Contains("CONECT", pdb);
        Assert.Contains("END", pdb);
    }

    [Fact]
    public void Generate3D_Methane_CalculatesTetrahedralGeometry()
    {
        var methane = Molecule.Parse("CH4");
        var m3d = methane.To3D();

        Assert.Equal("Tetrahedral", m3d.VseprShape);
        Assert.Equal(109.5, m3d.IdealBondAngleDegrees);
        Assert.Equal(5, m3d.Atoms.Count);
    }
}
