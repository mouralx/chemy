namespace Chemy.Core.Tests.ValidationData;

using System.IO;
using System.Text.Json;
using Chemy.Core;
using Chemy.Core.Graph;
using Chemy.Core.Pharmacology;
using Chemy.Core.Physics;
using Chemy.Core.Quantum;
using Chemy.Core.Reactions;
using Chemy.Core.Spectroscopy;
using Chemy.Core.Structure;
using Chemy.Core.Thermodynamics;
using Xunit;

/// <summary>
/// Machine-reproducible scientific benchmark validation suite.
/// Evaluates Chemy against a pinned reference dataset derived from published literature and standard chemoinformatics tools (RDKit 2024.03.1 / PubChem / NIST JANAF).
/// </summary>
public class ScientificBenchmarkValidationTests
{
    public record BenchmarkMoleculeRecord(
        string Id,
        string Name,
        string Smiles,
        string Formula,
        double ExactMolecularWeight,
        double ReferenceTpsa,
        double ReferenceLogP,
        double ReferenceQed,
        int ReferenceHbd,
        int ReferenceHba,
        int ReferenceRotatableBonds,
        int ReferenceAromaticRings,
        string Provenance
    );

    private static readonly Lazy<IReadOnlyList<BenchmarkMoleculeRecord>> LoadedBenchmarkDataset = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "ValidationData", "reference_compounds.json");
        if (!File.Exists(path))
        {
            // Fallback path search relative to test directory
            path = Path.Combine(Directory.GetCurrentDirectory(), "ValidationData", "reference_compounds.json");
        }

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<BenchmarkMoleculeRecord>>(json, options) ?? throw new InvalidOperationException("Failed to deserialize reference dataset.");
        }

        throw new FileNotFoundException($"Could not locate benchmark reference dataset at: {path}");
    });

    [Fact]
    public void Benchmark_Dataset_ContainsAtLeast15Compounds()
    {
        var dataset = LoadedBenchmarkDataset.Value;
        Assert.True(dataset.Count >= 15, $"Expected at least 15 reference compounds, found {dataset.Count}");
    }

    [Fact]
    public void Benchmark_MolecularWeightAndFormula_MatchesReferenceDataset()
    {
        foreach (var entry in LoadedBenchmarkDataset.Value)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            Assert.Equal(entry.Formula, mol.ChemicalFormula);
            Assert.InRange(mol.MolecularWeight, entry.ExactMolecularWeight - 0.2, entry.ExactMolecularWeight + 0.2);
        }
    }

    [Fact]
    public void Benchmark_ErtlTpsa_MatchesRDKitReferenceWithinStrictTolerance()
    {
        double totalAbsError = 0.0;
        var dataset = LoadedBenchmarkDataset.Value;

        foreach (var entry in dataset)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double actualTpsa = ErtlTpsa.Calculate(mol).TotalTpsa;
            double diff = Math.Abs(actualTpsa - entry.ReferenceTpsa);
            totalAbsError += diff;

            Assert.True(diff <= 0.1, $"TPSA difference {diff:F2} for {entry.Name} (actual: {actualTpsa:F2}, reference: {entry.ReferenceTpsa:F2}) exceeds tolerance.");
        }

        double mae = totalAbsError / dataset.Count;
        Assert.True(mae < 0.05, $"TPSA Mean Absolute Error {mae:F4} Å² exceeds strict threshold of 0.05 Å²");
    }

    [Fact]
    public void Benchmark_AromaticRings_SssrCycleBasis_MatchesReference()
    {
        foreach (var entry in LoadedBenchmarkDataset.Value)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            var sssr = CycleBasis.ComputeSssr(mol);
            int aromaticCount = sssr.Rings.Count(r => r.All(atomIdx => mol.Bonds.Any(b => b.Connects(atomIdx) && b.Type == BondType.Aromatic)));
            if (aromaticCount == 0 && mol.Bonds.Any(b => b.Type == BondType.Aromatic)) aromaticCount = 1;

            Assert.Equal(entry.ReferenceAromaticRings, aromaticCount);
        }
    }

    [Fact]
    public void Benchmark_HydrogenBondDonorsAndAcceptors_MatchesReference()
    {
        foreach (var entry in LoadedBenchmarkDataset.Value)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            var profile = AdmetEngine.Analyze(mol);

            Assert.Equal(entry.ReferenceHbd, profile.HydrogenBondDonors);
            Assert.Equal(entry.ReferenceHba, profile.HydrogenBondAcceptors);
        }
    }

    [Fact]
    public void Benchmark_RotatableBonds_MatchesReferenceExcludingAmidesAndTerminals()
    {
        foreach (var entry in LoadedBenchmarkDataset.Value)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            var profile = AdmetEngine.Analyze(mol);

            Assert.Equal(entry.ReferenceRotatableBonds, profile.RotatableBonds);
        }
    }

    [Fact]
    public void Benchmark_CrippenLogP_MatchesReferenceWithinSubsetModelTolerance()
    {
        double totalAbsError = 0.0;
        var dataset = LoadedBenchmarkDataset.Value;

        foreach (var entry in dataset)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double actualLogP = WildmanCrippenLogP.Calculate(mol).CalculatedLogP;
            double diff = Math.Abs(actualLogP - entry.ReferenceLogP);
            totalAbsError += diff;

            Assert.True(diff <= 1.0, $"LogP difference {diff:F2} for {entry.Name} (actual: {actualLogP:F2}, reference: {entry.ReferenceLogP:F2}) exceeds tolerance.");
        }

        double mae = totalAbsError / dataset.Count;
        Assert.True(mae < 0.35, $"LogP Mean Absolute Error {mae:F4} exceeds threshold of 0.35");
    }

    [Fact]
    public void Benchmark_QedDrugLikenessDesirability_EvaluatesWithinAcceptableMargin()
    {
        double totalAbsError = 0.0;
        var dataset = LoadedBenchmarkDataset.Value;

        foreach (var entry in dataset)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double actualQed = BickertonQed.Calculate(mol).QedScore;
            double diff = Math.Abs(actualQed - entry.ReferenceQed);
            totalAbsError += diff;

            Assert.True(diff <= 0.15, $"QED difference {diff:F3} for {entry.Name} (actual: {actualQed:F3}, reference: {entry.ReferenceQed:F3}) exceeds margin.");
        }

        double mae = totalAbsError / dataset.Count;
        Assert.True(mae < 0.08, $"QED Mean Absolute Error {mae:F4} exceeds threshold of 0.08");
    }

    [Fact]
    public void Benchmark_NistShomateThermodynamics_MatchesMultiTemperatureReferenceData()
    {
        // Reference NIST-JANAF thermochemical benchmark: (Formula, TempKelvin, ExpectedH_kJ, ExpectedS_JperK, HTol, STol)
        var referenceData = new (string Formula, double T, double ExpH, double ExpS, double HTol, double STol)[]
        {
            ("H2O(g)", 298.15, -241.83, 188.83, 0.5, 0.5),
            ("H2O(g)", 500.0,  -234.90, 206.53, 1.0, 1.0),
            ("CO2(g)", 298.15, -393.52, 213.79, 0.5, 0.5),
            ("CO2(g)", 600.0,  -380.60, 245.40, 1.5, 2.5),
            ("CH4(g)", 298.15, -74.87,  186.25, 0.5, 0.5),
            ("CH4(g)", 500.0,  -66.23,  207.72, 1.0, 1.5)
        };

        foreach (var (formula, temp, expH, expS, hTol, sTol) in referenceData)
        {
            var result = ShomateThermodynamics.Evaluate(formula, temp);
            Assert.NotNull(result);
            Assert.InRange(Math.Abs(result.StandardEnthalpyH - expH), 0.0, hTol);
            Assert.InRange(Math.Abs(result.StandardEntropyS - expS), 0.0, sTol);
        }
    }

    [Fact]
    public void Benchmark_HuckelMolecularOrbitals_MatchesAnalyticalEigenvalues()
    {
        // Analytical Hückel eigenvalues (x = (E - alpha)/beta)
        var ethylene = Molecule.FromSmiles("C=C", "Ethylene");
        var ethHmo = HuckelEngine.Analyze(ethylene);
        Assert.Equal(2, ethHmo.ConjugatedAtomCount);
        Assert.Equal(2.0, ethHmo.TotalPiEnergyBetaCoeff, precision: 3);

        var butadiene = Molecule.FromSmiles("C=CC=C", "1,3-Butadiene");
        var butHmo = HuckelEngine.Analyze(butadiene);
        Assert.Equal(4, butHmo.ConjugatedAtomCount);
        Assert.InRange(Math.Abs(butHmo.TotalPiEnergyBetaCoeff - 4.472), 0.0, 0.01);
        Assert.InRange(Math.Abs(butHmo.DewarResonanceEnergyBetaCoeff - 0.472), 0.0, 0.01);

        var benzene = Molecule.FromSmiles("c1ccccc1", "Benzene");
        var bzHmo = HuckelEngine.Analyze(benzene);
        Assert.Equal(6, bzHmo.ConjugatedAtomCount);
        Assert.Equal(8.0, bzHmo.TotalPiEnergyBetaCoeff, precision: 3);
        Assert.Equal(2.0, bzHmo.DewarResonanceEnergyBetaCoeff, precision: 3);
    }

    [Fact]
    public void Benchmark_FormulaVsTopologySeparation_ThrowsOnAllTopologyDependentEngines()
    {
        var formulaMol = Molecule.Parse("C9H8O4", "AspirinFormula");
        Assert.False(formulaMol.HasBondedTopology);

        // Every topology-dependent engine MUST reject unbonded formulas
        Assert.Throws<InvalidOperationException>(() => AdmetEngine.Analyze(formulaMol));
        Assert.Throws<InvalidOperationException>(() => ErtlTpsa.Calculate(formulaMol));
        Assert.Throws<InvalidOperationException>(() => WildmanCrippenLogP.Calculate(formulaMol));
        Assert.Throws<InvalidOperationException>(() => BickertonQed.Calculate(formulaMol));
        Assert.Throws<InvalidOperationException>(() => SpectroscopyEngine.Predict(formulaMol));
        Assert.Throws<InvalidOperationException>(() => ChemicalGraph.FromMolecule(formulaMol));
        Assert.Throws<InvalidOperationException>(() => formulaMol.To3D());
    }
}
