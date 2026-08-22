using Chemy.Core.Electrochemistry;
using Chemy.Core.Evolution;
using Chemy.Core.Kinetics;
using Chemy.Core.Pharmacology;
using Chemy.Core.Physics;
using Chemy.Core.Quantum;
using Chemy.Core.Scientific;
using Chemy.Core.Spectroscopy;
using Chemy.Core.Thermodynamics;
using Xunit;

namespace Chemy.Core.Tests;

public class ScientificContractsTests
{
    [Fact]
    public void PredictiveModels_UnsupportedElement_FailClosedWithOutOfDomainAssessment()
    {
        var molecule = new Molecule(
            "Silyl",
            [new Atom(Elements.Silicon, 14), new Atom(Elements.Hydrogen, 0)],
            [new Bond(0, 1, BondType.Single)]);

        var forceFieldAssessment = ForceFieldEngine.AssessApplicability(molecule);
        Assert.Equal(ApplicabilityStatus.OutOfDomain, forceFieldAssessment.Status);
        Assert.Contains(forceFieldAssessment.Reasons, reason => reason.Contains("Si", StringComparison.Ordinal));

        Assert.Throws<NotSupportedException>(() => WildmanCrippenLogP.Calculate(molecule));
        Assert.Throws<NotSupportedException>(() => ErtlTpsa.Calculate(molecule));
        Assert.Throws<NotSupportedException>(() => BickertonQed.Calculate(molecule));
        Assert.Throws<NotSupportedException>(() => AdmetEngine.Analyze(molecule));
        Assert.Throws<NotSupportedException>(() => SpectroscopyEngine.Predict(molecule));
        Assert.Throws<NotSupportedException>(() => HuckelEngine.Analyze(molecule));
    }

    [Fact]
    public void DescriptorResults_ExposeCalibrationAndCertificationState()
    {
        var ethanol = Molecule.FromSmiles("CCO", "Ethanol");
        var profile = AdmetEngine.Analyze(ethanol);

        Assert.Equal(ApplicabilityStatus.InDomain, profile.Applicability.Status);
        Assert.Equal(3, profile.DescriptorUncertainty.Count);
        Assert.NotNull(profile.MethodInfo.ValidationEvidence);
        Assert.Equal(48, profile.MethodInfo.ValidationEvidence.SampleSize);
        Assert.False(profile.MethodInfo.ValidationEvidence.IndependentlyCurated);
        Assert.False(profile.MethodInfo.ValidationEvidence.ProspectivelyFrozen);
        Assert.All(profile.DescriptorUncertainty.Values, uncertainty =>
        {
            Assert.True(uncertainty.AbsoluteErrorEnvelope >= 0.0);
            Assert.InRange(uncertainty.CoverageFraction, 0.0, 1.0);
        });
    }

    [Fact]
    public void ForceFieldGradient_IsTranslationInvariantAndDoesNotMutateCoordinates()
    {
        var molecule = Molecule.FromSmiles("CCO", "Ethanol").To3D();
        var before = molecule.Atoms.Select(atom => atom.Position).ToArray();

        var gradient = ForceFieldEngine.CalculateGradient(molecule);

        Assert.Equal(ApplicabilityStatus.InDomain, gradient.Applicability.Status);
        Assert.Equal(before, molecule.Atoms.Select(atom => atom.Position).ToArray());
        Assert.InRange(Math.Abs(gradient.CartesianGradientKcalPerMolAngstrom.Sum(vector => vector.X)), 0.0, 1e-6);
        Assert.InRange(Math.Abs(gradient.CartesianGradientKcalPerMolAngstrom.Sum(vector => vector.Y)), 0.0, 1e-6);
        Assert.InRange(Math.Abs(gradient.CartesianGradientKcalPerMolAngstrom.Sum(vector => vector.Z)), 0.0, 1e-6);
        Assert.Throws<ArgumentOutOfRangeException>(() => ForceFieldEngine.CalculateGradient(molecule, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ForceFieldEngine.CalculateGradient(molecule, double.NaN));
    }

    [Fact]
    public void ReactionNetwork_ReportsAnalyticalResidualAndConservation()
    {
        var result = ReactionNetworkEngine.SimulateConsecutiveCascade(
            initialConcA: 1.0,
            k1: 0.5,
            k2: 0.2,
            totalTime: 10.0,
            steps: 200);

        Assert.True(result.Diagnostics.Converged);
        Assert.Equal(0.05, result.Diagnostics.StepSize, precision: 12);
        Assert.InRange(result.Diagnostics.MaximumResidual, 0.0, 1e-7);
        Assert.InRange(result.Diagnostics.MaximumConservationError, 0.0, 1e-12);
        Assert.Equal(201, result.Points.Count);
        Assert.Equal(10.0, result.Points[^1].TimeSeconds, precision: 12);
    }

    [Fact]
    public void GeneralReactionNetwork_InvalidNumericsFailClosed()
    {
        Assert.Throws<InvalidOperationException>(() => ReactionNetworkEngine.SimulateGeneralNetwork(
            [1.0, 0.0],
            _ => [double.NaN, 0.0],
            totalTime: 1.0,
            steps: 10));

        Assert.Throws<InvalidOperationException>(() => ReactionNetworkEngine.SimulateGeneralNetwork(
            [1.0, 0.0],
            _ => [0.0],
            totalTime: 1.0,
            steps: 10));
    }

    [Fact]
    public void StandardStateThermodynamics_RejectsUnsupportedTemperatureAndSilentIsomerSubstitution()
    {
        var combustion = Reaction.Parse("CH4 + O2 -> CO2 + H2O");
        Assert.Throws<NotSupportedException>(() => ThermodynamicsEngine.GetThermodynamics(combustion, 350.0));

        var isobutane = Molecule.FromSmiles("CC(C)C", "Isobutane");
        var isomerReaction = new Reaction(
            [new ReactionComponent(isobutane, 2), new ReactionComponent(Molecule.Parse("O2", "O2"), 13)],
            [new ReactionComponent(Molecule.Parse("CO2", "CO2"), 8), new ReactionComponent(Molecule.Parse("H2O", "H2O"), 10)]);

        Assert.Throws<KeyNotFoundException>(() => ThermodynamicsEngine.GetThermodynamics(isomerReaction));
        var estimated = ThermodynamicsEngine.GetThermodynamics(isomerReaction, allowBensonEstimates: true);
        Assert.Equal(ApplicabilityStatus.Boundary, estimated.Applicability.Status);
        Assert.Equal("Benson group-additivity estimate", estimated.PropertySources["C4H10"]);
    }

    [Fact]
    public void ExactEquationResults_ExposeScientificScopeAndRejectNonFiniteInputs()
    {
        var nernst = ElectrochemistryEngine.CalculateNernstPotential(1.103, 2, 0.01, 298.15);
        Assert.Equal(EvidenceLevel.ExactEquation, nernst.MethodInfo.EvidenceLevel);
        Assert.NotEmpty(nernst.MethodInfo.Warnings);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ElectrochemistryEngine.CalculateNernstPotential(1.0, 1, double.NaN, 298.15));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ElectrochemistryEngine.CalculateNernstPotential(1.0, 1, 1.0, 0.0));
    }

    [Fact]
    public void HuckelMatrix_RejectsAsymmetricOrNonFiniteHamiltonians()
    {
        Assert.Throws<ArgumentException>(() => HuckelEngine.AnalyzeMatrix(
            "Asymmetric",
            new[,] { { 0.0, 1.0 }, { 0.9, 0.0 } },
            [1, 1]));
        Assert.Throws<ArgumentException>(() => HuckelEngine.AnalyzeMatrix(
            "NonFinite",
            new[,] { { 0.0, double.NaN }, { double.NaN, 0.0 } },
            [1, 1]));
    }

    [Fact]
    public void MolecularEvolution_RejectsFormulaOnlyAndInvalidGenerationBudget()
    {
        Assert.Throws<FormatException>(() => MolecularEvolverEngine.EvolveLeadCandidate("C6H12O6", 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => MolecularEvolverEngine.EvolveLeadCandidate("CCO", 0));

        var result = MolecularEvolverEngine.EvolveLeadCandidate("CCO", 5);
        Assert.Equal(EvidenceLevel.Heuristic, result.MethodInfo.EvidenceLevel);
        Assert.NotEmpty(result.MethodInfo.Warnings);
        Assert.True(result.Applicability.IsWithinDomain);
    }
}
