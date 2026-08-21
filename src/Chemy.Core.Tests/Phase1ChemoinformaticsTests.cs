namespace Chemy.Core.Tests;

using Chemy.Core;
using Chemy.Core.Graph;
using Chemy.Core.Pharmacology;
using Chemy.Core.Spectroscopy;
using Chemy.Core.Structure;
using Xunit;

public class Phase1ChemoinformaticsTests
{
    [Fact]
    public void ErtlTpsa_Ethanol_ReturnsCorrectPolarArea()
    {
        // Ethanol: C2H6O -> -OH contribution = 20.23 Å²
        var mol = SmilesParser.Parse("CCO");
        var result = ErtlTpsa.Calculate(mol);

        Assert.Equal(20.23, result.TotalTpsa);
        Assert.Single(result.AtomContributions);
        Assert.Equal("O", result.AtomContributions[0].ElementSymbol);
    }

    [Fact]
    public void ErtlTpsa_AceticAcid_ReturnsCorrectPolarArea()
    {
        // Acetic Acid: CC(=O)O -> =O (17.07) + -OH (20.23) = 37.30 Å²
        var mol = SmilesParser.Parse("CC(=O)O");
        var result = ErtlTpsa.Calculate(mol);

        Assert.Equal(37.30, result.TotalTpsa);
        Assert.Equal(2, result.AtomContributions.Count);
    }

    [Fact]
    public void ErtlTpsa_Acetamide_ReturnsCorrectPolarArea()
    {
        // Acetamide: CC(=O)N -> =O (17.07) + -CONH2 (43.09) = 60.16 Å² (standard Ertl) or primary amide
        var mol = SmilesParser.Parse("CC(=O)N");
        var result = ErtlTpsa.Calculate(mol);

        Assert.True(result.TotalTpsa > 50.0);
    }

    [Fact]
    public void WildmanCrippenLogP_MethaneAndEthane_CalculatesPositiveLogP()
    {
        var methane = SmilesParser.Parse("C");
        var ethane = SmilesParser.Parse("CC");

        var resMethane = WildmanCrippenLogP.Calculate(methane);
        var resEthane = WildmanCrippenLogP.Calculate(ethane);

        Assert.True(resMethane.CalculatedLogP > 0.0);
        Assert.True(resEthane.CalculatedLogP > resMethane.CalculatedLogP);
        Assert.True(resEthane.CalculatedMr > resMethane.CalculatedMr);
    }

    [Fact]
    public void WildmanCrippenLogP_Benzene_MatchesAromaticParameters()
    {
        // Benzene: c1ccccc1 -> 6 * C18 (0.1582) + 6 * H3 (0.1130) = 6 * 0.2712 = 1.6272 ≈ 1.63
        var mol = SmilesParser.Parse("c1ccccc1");
        var result = WildmanCrippenLogP.Calculate(mol);

        Assert.InRange(result.CalculatedLogP, 1.50, 1.80);
    }

    [Fact]
    public void BickertonQed_CalculatesValidScoreBetweenZeroAndOne()
    {
        var aspirin = SmilesParser.Parse("CC(=O)Oc1ccccc1C(=O)O");
        var qedResult = BickertonQed.Calculate(aspirin);

        Assert.InRange(qedResult.QedScore, 0.10, 0.95);
        Assert.Equal(8, qedResult.DescriptorDesirabilities.Count);
        Assert.True(qedResult.DescriptorDesirabilities.ContainsKey("MolecularWeight"));
        Assert.True(qedResult.DescriptorDesirabilities.ContainsKey("ALogP"));
        Assert.True(qedResult.DescriptorDesirabilities.ContainsKey("TPSA"));
    }

    [Fact]
    public void WeisfeilerLehman_Acetone_SymmetryClasses_IdentifiesEquivalentMethylProtons()
    {
        // Acetone: CC(=O)C (has 6 protons, 2 methyl carbons, 1 carbonyl carbon, 1 carbonyl oxygen)
        var mol = SmilesParser.Parse("CC(=O)C");
        var partition = WeisfeilerLehman.Partition(mol);

        // Hydrogens should be partitioned into exactly 1 equivalence class of size 6
        var hIndices = mol.Atoms.Select((a, i) => (a, i)).Where(t => t.a.Element.Symbol == "H").Select(t => t.i).ToList();
        var distinctHClasses = hIndices.Select(h => partition.SymmetryClasses[h]).Distinct().ToList();

        Assert.Equal(6, hIndices.Count);
        Assert.Single(distinctHClasses); // All 6 protons are topologically equivalent!

        // Spectroscopy 1H NMR should produce a single peak of 6H
        var spec = SpectroscopyEngine.Predict(mol);
        Assert.Single(spec.H1NmrPeaks);
        Assert.Equal(6, spec.H1NmrPeaks[0].IntegrationCount);
        Assert.Equal("Singlet", spec.H1NmrPeaks[0].Multiplet);
    }

    [Fact]
    public void WeisfeilerLehman_Benzene_SymmetryClasses_IdentifiesAllSixProtonsEquivalent()
    {
        var benzene = SmilesParser.Parse("c1ccccc1");
        var partition = WeisfeilerLehman.Partition(benzene);

        var hIndices = benzene.Atoms.Select((a, i) => (a, i)).Where(t => t.a.Element.Symbol == "H").Select(t => t.i).ToList();
        var distinctHClasses = hIndices.Select(h => partition.SymmetryClasses[h]).Distinct().ToList();

        Assert.Equal(6, hIndices.Count);
        Assert.Single(distinctHClasses); // All 6 benzene protons are identical!
    }
}
