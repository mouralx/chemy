using Chemy.Core;
using Chemy.Core.Pharmacology;
using Xunit;

namespace Chemy.Core.Tests;

public class PharmacologyTests
{
    [Fact]
    public void Analyze_Aspirin_ComputesValidAdmetProfile()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var admet = AdmetEngine.Analyze(aspirin);

        Assert.NotNull(admet);
        Assert.True(admet.MolecularWeight > 170.0 && admet.MolecularWeight < 190.0);
        Assert.True(admet.TpsaAngstrom2 >= 50.0 && admet.TpsaAngstrom2 <= 70.0);
        Assert.True(admet.QedDrugLikenessScore > 0.4);
        Assert.True(admet.PassesLipinskiRuleOf5);
    }

    [Fact]
    public void Analyze_Paracetamol_ComputesValidProperties()
    {
        var paracetamol = Molecule.FromSmiles("CC(=O)Nc1ccc(O)cc1", "Paracetamol");
        var admet = AdmetEngine.Analyze(paracetamol);

        Assert.NotNull(admet);
        Assert.True(admet.MolecularWeight > 145.0 && admet.MolecularWeight < 155.0);
        Assert.True(admet.HydrogenBondDonors == 2); // -OH and -NH-
        Assert.Equal(49.3, admet.TpsaAngstrom2); // Published Ertl reference: 49.33 Å² (C=O: 17.07 + CONH: 12.03 + OH: 20.23)
        Assert.True(admet.QedDrugLikenessScore > 0.5);
    }

    [Fact]
    public void CalculateCrippenLogP_MatchesExpectedPhysicalRanges()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var logP = AdmetEngine.CalculateCrippenLogP(aspirin);

        Assert.True(logP > 0.5 && logP < 2.5); // Experimental LogP ~ 1.19
    }

    [Fact]
    public void Analyze_AromaticNitrogenHeterocycles_AccurateHBondDonors()
    {
        var pyridine = Molecule.FromSmiles("c1ccncc1", "Pyridine");
        var pyridineAdmet = AdmetEngine.Analyze(pyridine);
        Assert.Equal(0, pyridineAdmet.HydrogenBondDonors);
        Assert.Equal(1, pyridineAdmet.HydrogenBondAcceptors);

        var pyrrole = Molecule.FromSmiles("c1cc[nH]c1", "Pyrrole");
        var pyrroleAdmet = AdmetEngine.Analyze(pyrrole);
        Assert.Equal(1, pyrroleAdmet.HydrogenBondDonors);
    }
}
