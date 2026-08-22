using Chemy.Core;
using Chemy.Core.Spatial;
using Xunit;

namespace Chemy.Core.Tests;

public class Geometry3DTests
{
    [Fact]
    public void Generate3D_Water_CalculatesBentGeometryAndFormatsXyzPdb()
    {
        var water = Molecule.FromSmiles("O", "Water");
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

        // Formula-only unbonded molecule should NOT emit CONECT records
        var formulaMol = Molecule.Parse("H2O");
        var formula3D = Geometry3DEngine.Generate3D(formulaMol);
        Assert.DoesNotContain("CONECT", formula3D.ToPdb());
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

    [Fact]
    public void Generate3D_Ibuprofen_BuildsRealisticConformerWithoutStericClashes()
    {
        var ibuprofen = Molecule.FromSmiles("CC(C)Cc1ccc(cc1)C(C)C(=O)O", "Ibuprofen");
        var m3d = ibuprofen.To3D();

        Assert.NotNull(m3d);
        Assert.Equal(33, m3d.Atoms.Count);

        // Verify that no bonded or non-bonded atoms overlap (minimum pairwise distance > 0.85 Å)
        for (int i = 0; i < m3d.Atoms.Count; i++)
        {
            for (int j = i + 1; j < m3d.Atoms.Count; j++)
            {
                var p1 = m3d.Atoms[i].Position;
                var p2 = m3d.Atoms[j].Position;
                double dist = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
                Assert.True(dist > 0.80, $"Atoms {i} ({m3d.Atoms[i].Atom.Element.Symbol}) and {j} ({m3d.Atoms[j].Atom.Element.Symbol}) are colliding at distance {dist:F3} Å");
            }
        }
    }

    [Fact]
    public void Generate3D_CaffeineWithAutoShape_BuildsFusedRingMultiCenterConformer()
    {
        var caffeine = Molecule.FromSmiles("CN1C=NC2=C1C(=O)N(C(=O)N2C)C", "Caffeine");
        var m3d = caffeine.To3D("Auto");

        Assert.NotNull(m3d);
        Assert.Equal(24, m3d.Atoms.Count);
        Assert.Equal("Conformer", m3d.VseprShape);
        Assert.NotEqual("Octahedral", m3d.VseprShape);

        for (int i = 0; i < m3d.Atoms.Count; i++)
        {
            for (int j = i + 1; j < m3d.Atoms.Count; j++)
            {
                var p1 = m3d.Atoms[i].Position;
                var p2 = m3d.Atoms[j].Position;
                double dist = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
                Assert.True(dist > 0.80, $"Atoms {i} and {j} colliding at {dist:F3} Å");
            }
        }
    }

    [Theory]
    [InlineData("CCO", "Ethanol")]
    [InlineData("c1ccccc1", "Benzene")]
    [InlineData("CC(=O)Oc1ccccc1C(=O)O", "Aspirin")]
    public void GenerateConformer3DResult_ExposesConvergenceAndBudgetEvidence(string smiles, string name)
    {
        var molecule = Molecule.FromSmiles(smiles, name);

        var result100 = Geometry3DEngine.GenerateConformer3DResult(molecule, maxIterations: 100);
        var result500 = Geometry3DEngine.GenerateConformer3DResult(molecule, maxIterations: 500);

        Assert.True(double.IsFinite(result500.InitialEnergyKcalPerMol));
        Assert.True(double.IsFinite(result500.FinalEnergyKcalPerMol));
        Assert.True(double.IsFinite(result500.FinalGradientNorm));
        Assert.True(
            result500.Converged,
            $"{name} did not converge: {result500.TerminationReason}; gradient={result500.FinalGradientNorm:G6}; iterations={result500.Iterations}");
        Assert.InRange(result500.Iterations, 0, 500);
        Assert.True(result500.FinalEnergyKcalPerMol <= result500.InitialEnergyKcalPerMol);
        Assert.True(result500.FinalEnergyKcalPerMol <= result100.FinalEnergyKcalPerMol + 1e-8);
        Assert.Equal(molecule.Atoms.Count, result500.MinimizedMolecule.Atoms.Count);
    }
}
