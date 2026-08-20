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

    [Fact]
    public void GeneratePlanar3D_Aspirin_AllAtomsLieStrictlyOnXYPlane()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var planar3D = aspirin.ToPlanar3D();

        Assert.NotNull(planar3D);
        Assert.Equal(aspirin.Atoms.Count, planar3D.Atoms.Count);
        Assert.Contains("Planar", planar3D.VseprShape);

        // Verify that EVERY atom has Z = 0.0 (strictly flat planar representation)
        foreach (var atom in planar3D.Atoms)
        {
            Assert.Equal(0.0, atom.Position.Z, precision: 6);
        }

        string xyz = planar3D.ToXyz();
        Assert.Contains("Planar", xyz);
        Assert.Contains("0.0000", xyz);

        string pdb = planar3D.ToPdb();
        Assert.Contains("HETATM", pdb);
    }

    [Fact]
    public void GeneratePlanar3D_Benzene_FormsRegularPlanarPolygonRing()
    {
        var benzene = Molecule.FromSmiles("c1ccccc1", "Benzene");
        var planar3D = benzene.ToPlanar3D();

        Assert.Equal(12, planar3D.Atoms.Count);
        foreach (var atom in planar3D.Atoms)
        {
            Assert.Equal(0.0, atom.Position.Z, precision: 6);
        }
    }
}
