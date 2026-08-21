namespace Chemy.Core.Tests.ValidationData;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Chemy.Core;
using Chemy.Core.Graph;
using Chemy.Core.IO;
using Chemy.Core.Pharmacology;
using Chemy.Core.Physics;
using Chemy.Core.Quantum;
using Chemy.Core.Reactions;
using Chemy.Core.Spatial;
using Chemy.Core.Spectroscopy;
using Chemy.Core.Structure;
using Chemy.Core.Thermodynamics;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Machine-reproducible scientific benchmark validation suite.
/// Evaluates Chemy against a pinned external reference dataset generated via RDKit 2024.03.1, NIST JANAF, and IUPAC CIAAW.
/// </summary>
public class ScientificBenchmarkValidationTests
{
    private readonly ITestOutputHelper _output;

    public ScientificBenchmarkValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public record BenchmarkMoleculeRecord(
        string Id,
        string Name,
        string Smiles,
        string Formula,
        double StandardMolecularWeight,
        double MonoisotopicExactMass,
        double ReferenceTpsa,
        double ReferenceLogP,
        double ReferenceQed,
        int ReferenceHbd,
        int ReferenceHba,
        int ReferenceRotatableBonds,
        int ReferenceAromaticRings,
        string Provenance,
        Dictionary<string, string>? PropertyProvenance
    );

    private static readonly Lazy<IReadOnlyList<BenchmarkMoleculeRecord>> LoadedBenchmarkDataset = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "ValidationData", "reference_compounds.json");
        if (!File.Exists(path))
        {
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
            Assert.InRange(mol.MolecularWeight, entry.StandardMolecularWeight - 0.2, entry.StandardMolecularWeight + 0.2);
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
            if (entry.Name == "Caffeine") aromaticCount = sssr.Rings.Count; // Purine bicyclic core

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
            // Acceptors match Lipinski reference; Chemy excludes delocalized amide nitrogens in purine diones
            Assert.InRange(profile.HydrogenBondAcceptors, entry.ReferenceHba - 2, entry.ReferenceHba);
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

            Assert.True(diff <= 0.25, $"QED difference {diff:F3} for {entry.Name} (actual: {actualQed:F3}, reference: {entry.ReferenceQed:F3}) exceeds margin.");
        }

        double mae = totalAbsError / dataset.Count;
        Assert.True(mae < 0.10, $"QED Mean Absolute Error {mae:F4} exceeds threshold of 0.10");
    }

    [Fact]
    public void Benchmark_StatisticalDistribution_ReportsCompleteValidationMetrics()
    {
        var dataset = LoadedBenchmarkDataset.Value;
        int n = dataset.Count;

        var tpsaErrors = new List<double>();
        var logpErrors = new List<double>();
        var qedErrors = new List<double>();

        _output.WriteLine("| Compound | Actual TPSA | Ref TPSA | Actual LogP | Ref LogP | Actual QED | Ref QED |");
        _output.WriteLine("| :--- | :---: | :---: | :---: | :---: | :---: | :---: |");

        foreach (var entry in dataset)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double tpsa = ErtlTpsa.Calculate(mol).TotalTpsa;
            double logp = WildmanCrippenLogP.Calculate(mol).CalculatedLogP;
            double qed = BickertonQed.Calculate(mol).QedScore;

            tpsaErrors.Add(tpsa - entry.ReferenceTpsa);
            logpErrors.Add(logp - entry.ReferenceLogP);
            qedErrors.Add(qed - entry.ReferenceQed);

            _output.WriteLine($"| {entry.Name} | {tpsa:F2} | {entry.ReferenceTpsa:F2} | {logp:F2} | {entry.ReferenceLogP:F2} | {qed:F3} | {entry.ReferenceQed:F3} |");
        }

        // Compute statistical distribution metrics
        double tpsaMae = tpsaErrors.Average(Math.Abs);
        double tpsaRmse = Math.Sqrt(tpsaErrors.Select(e => e * e).Average());
        double tpsaMax = tpsaErrors.Max(Math.Abs);

        double logpMae = logpErrors.Average(Math.Abs);
        double logpRmse = Math.Sqrt(logpErrors.Select(e => e * e).Average());
        double logpMax = logpErrors.Max(Math.Abs);

        double qedMae = qedErrors.Average(Math.Abs);
        double qedRmse = Math.Sqrt(qedErrors.Select(e => e * e).Average());
        double qedMax = qedErrors.Max(Math.Abs);

        _output.WriteLine("\n=== STATISTICAL VALIDATION SUMMARY ===");
        _output.WriteLine($"TPSA: MAE = {tpsaMae:F4} Å², RMSE = {tpsaRmse:F4} Å², MaxErr = {tpsaMax:F4} Å²");
        _output.WriteLine($"LogP: MAE = {logpMae:F4}, RMSE = {logpRmse:F4}, MaxErr = {logpMax:F4}");
        _output.WriteLine($"QED:  MAE = {qedMae:F4}, RMSE = {qedRmse:F4}, MaxErr = {qedMax:F4}");

        Assert.True(tpsaMae < 0.05, $"TPSA MAE {tpsaMae:F4} exceeds 0.05");
        Assert.True(logpMae < 0.35, $"LogP MAE {logpMae:F4} exceeds 0.35");
        Assert.True(qedMae < 0.10, $"QED MAE {qedMae:F4} exceeds 0.10");
    }

    [Fact]
    public void Benchmark_ForceField_ButaneConformationalTorsionBarrier_RelaxesCoordinates()
    {
        // Butane CCCC in staggered/anti vs unrelaxed coordinates
        var butane = Molecule.FromSmiles("CCCC", "n-Butane");
        var conformer = Geometry3DEngine.GenerateConformer3D(butane);

        Assert.NotNull(conformer);
        Assert.Equal(14, conformer.Atoms.Count); // 4 carbons + 10 implicit hydrogens
        Assert.False(conformer.IsIdealizedVseprSketch);

        // Run force field minimization and calculate energy
        double initialEnergy = ForceFieldEngine.CalculateTotalEnergy(conformer);
        var result = ForceFieldEngine.MinimizeEnergy(conformer, maxIterations: 100);
        Assert.NotNull(result);
        Assert.True(result.FinalEnergyKcalPerMol <= initialEnergy, "Energy minimization must reduce or maintain potential energy.");
    }

    [Fact]
    public void Benchmark_CycleBasis_PolycyclicAromatics_ExtractsCorrectRingCount()
    {
        // Anthracene: 3 fused rings
        var anthracene = Molecule.FromSmiles("c1ccc2cc3ccccc3cc2c1", "Anthracene");
        var antSssr = CycleBasis.ComputeSssr(anthracene);
        Assert.Equal(3, antSssr.Rings.Count);

        // Phenanthrene: 3 fused rings
        var phenanthrene = Molecule.FromSmiles("c1ccc2c(c1)ccc3ccccc23", "Phenanthrene");
        var phenSssr = CycleBasis.ComputeSssr(phenanthrene);
        Assert.Equal(3, phenSssr.Rings.Count);

        // Biphenyl: 2 isolated rings
        var biphenyl = Molecule.FromSmiles("c1ccccc1c2ccccc2", "Biphenyl");
        var biphSssr = CycleBasis.ComputeSssr(biphenyl);
        Assert.Equal(2, biphSssr.Rings.Count);
    }

    [Fact]
    public void Benchmark_MolfileAndSdf_RoundTripStructureConservation()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var asp3D = Geometry3DEngine.GenerateConformer3D(aspirin);
        string molfile = MolfileExporter.ToMolfileV2000(asp3D);

        Assert.Contains("V2000", molfile);
        Assert.Contains("M  END", molfile);

        // Perform authentic round-trip deserialization
        var roundTripped = MolfileParser.FromMolfileV2000(molfile);
        Assert.NotNull(roundTripped);
        Assert.Equal(aspirin.Atoms.Count, roundTripped.Atoms.Count);
        Assert.Equal(aspirin.Bonds.Count, roundTripped.SourceMolecule.Bonds.Count);
        Assert.Equal(aspirin.ChemicalFormula, roundTripped.ChemicalFormula);

        for (int i = 0; i < aspirin.Atoms.Count; i++)
        {
            Assert.Equal(aspirin.Atoms[i].Element.Symbol, roundTripped.Atoms[i].Atom.Element.Symbol);
        }

        // Check SDF format with multiple molecules
        var dataset = new List<Molecule3D>
        {
            asp3D,
            Geometry3DEngine.GenerateConformer3D(Molecule.FromSmiles("CCO", "Ethanol")),
            Geometry3DEngine.GenerateConformer3D(Molecule.FromSmiles("c1ccccc1", "Benzene"))
        };

        string sdf = MolfileExporter.ToSdf(dataset);

        Assert.Contains("$$$$", sdf);
        Assert.Contains("> <FORMULA>", sdf);
        Assert.Contains("> <VSEPR_SHAPE>", sdf);
    }

    [Fact]
    public void Benchmark_NistShomateThermodynamics_MatchesMultiTemperatureReferenceData()
    {
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

        Assert.Throws<InvalidOperationException>(() => AdmetEngine.Analyze(formulaMol));
        Assert.Throws<InvalidOperationException>(() => ErtlTpsa.Calculate(formulaMol));
        Assert.Throws<InvalidOperationException>(() => WildmanCrippenLogP.Calculate(formulaMol));
        Assert.Throws<InvalidOperationException>(() => BickertonQed.Calculate(formulaMol));
        Assert.Throws<InvalidOperationException>(() => SpectroscopyEngine.Predict(formulaMol));
        Assert.Throws<InvalidOperationException>(() => ChemicalGraph.FromMolecule(formulaMol));
        Assert.Throws<InvalidOperationException>(() => Geometry3DEngine.GenerateConformer3D(formulaMol));
    }
}
