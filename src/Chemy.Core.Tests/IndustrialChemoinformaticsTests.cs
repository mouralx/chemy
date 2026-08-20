using Chemy.Core;
using Chemy.Core.Evolution;
using Chemy.Core.Graph;
using Chemy.Core.IO;
using Chemy.Core.Pharmacology;
using Chemy.Core.Physics;
using Xunit;

namespace Chemy.Core.Tests;

public class IndustrialChemoinformaticsTests
{
    [Fact]
    public void ChemicalGraph_CycleDetection_FindsAromaticRings()
    {
        var benzene = Molecule.FromSmiles("c1ccccc1", "Benzene");
        var graph = ChemicalGraph.FromMolecule(benzene);

        Assert.Equal(benzene.Atoms.Count, graph.NodeCount);
        Assert.Equal(benzene.Bonds.Count, graph.EdgeCount);

        var rings = graph.FindRings();
        Assert.NotEmpty(rings);
        Assert.Contains(rings, r => r.Count == 6);
    }

    [Fact]
    public void SubgraphMatcher_FindsCarboxylicAcidMotif()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var graph = ChemicalGraph.FromMolecule(aspirin);

        var matches = SubgraphMatcher.FindMatches(graph, SubgraphMatcher.CarboxylicAcidQuery);
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void GraphRewriter_ReplacesCarboxylWithTetrazole_PreservesConnectivity()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var tetrazoleLead = GraphRewriter.ReplaceCarboxylWithTetrazole(aspirin);

        Assert.NotNull(tetrazoleLead);
        Assert.Contains(tetrazoleLead.Atoms, a => a.Element.Symbol == "N");
        Assert.True(tetrazoleLead.Atoms.Count > aspirin.Atoms.Count);
    }

    [Fact]
    public void ForceFieldEngine_MultiTermMinimization_ReducesEnergy()
    {
        var water = Molecule.Water.To3D();
        var result = ForceFieldEngine.MinimizeEnergy(water, maxIterations: 30);

        Assert.NotNull(result);
        Assert.True(result.FinalEnergyKcalPerMol <= result.InitialEnergyKcalPerMol);
        Assert.InRange(result.Iterations, 0, 30);
        Assert.Equal(water.Atoms.Count, result.MinimizedMolecule.Atoms.Count);
    }

    [Fact]
    public void AdmetEngine_ErtlTpsaAndVeberRules_EvaluatesCorrectly()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        Assert.Equal("C9H8O4", aspirin.ChemicalFormula);
        Assert.Equal(180.16, Math.Round(aspirin.MolecularWeight, 2));

        var profile = AdmetEngine.Analyze(aspirin);

        Assert.Equal(1, profile.AromaticRings);
        Assert.True(profile.RotatableBonds is 2 or 3);
        Assert.True(profile.TpsaAngstrom2 > 40.0 && profile.TpsaAngstrom2 < 80.0);
        Assert.True(profile.PassesLipinskiRuleOf5);
        Assert.True(profile.PassesVeberRules);
        Assert.True(profile.PassesGhoseFilter);
        Assert.True(profile.QedDrugLikenessScore > 0.4);
    }

    [Fact]
    public void MolfileExporter_ExportsValidV2000Format()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin").To3D();
        string molfile = MolfileExporter.ToMolfileV2000(aspirin);

        Assert.Contains("V2000", molfile);
        Assert.Contains("M  END", molfile);
        Assert.Contains("C", molfile);
        Assert.Contains("O", molfile);
    }

    [Fact]
    public void MolfileExporter_ExportsValidSdfFormat()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin").To3D();
        var caffeine = Molecule.FromSmiles("CN1C=NC2=C1C(=O)N(C(=O)N2C)C", "Caffeine").To3D();

        string sdf = MolfileExporter.ToSdf([aspirin, caffeine]);

        Assert.Contains("$$$$", sdf);
        Assert.Contains("> <FORMULA>", sdf);
    }
}
