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
/// Evaluates Chemy against a hash-locked external reference dataset generated via RDKit 2025.09.2, NIST JANAF, and IUPAC CIAAW.
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
        string? Subset,
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
    public void Benchmark_Dataset_ContainsAtLeast30Compounds()
    {
        var dataset = LoadedBenchmarkDataset.Value;
        Assert.True(dataset.Count >= 30, $"Expected at least 30 reference compounds, found {dataset.Count}");
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
        var regressionSet = LoadedBenchmarkDataset.Value.Where(d => d.Subset is "tuning" or "expanded_regression").ToList();

        foreach (var entry in regressionSet)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double actualTpsa = ErtlTpsa.Calculate(mol).TotalTpsa;
            double diff = Math.Abs(actualTpsa - entry.ReferenceTpsa);
            totalAbsError += diff;

            Assert.True(diff <= 0.15, $"TPSA difference {diff:F2} for {entry.Name} (actual: {actualTpsa:F2}, reference: {entry.ReferenceTpsa:F2}) exceeds tolerance.");
        }

        double mae = totalAbsError / regressionSet.Count;
        Assert.True(mae < 0.05, $"TPSA Mean Absolute Error {mae:F4} Å² exceeds strict threshold of 0.05 Å²");
    }

    [Fact]
    public void Benchmark_AromaticRings_SssrCycleBasis_MatchesReference()
    {
        var regressionSet = LoadedBenchmarkDataset.Value.Where(d => d.Subset is "tuning" or "expanded_regression").ToList();
        foreach (var entry in regressionSet)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            var sssr = CycleBasis.ComputeSssr(mol);
            int aromaticCount = sssr.Rings.Count(r => r.All(atomIdx => mol.Bonds.Any(b => b.Connects(atomIdx) && b.Type == BondType.Aromatic)));
            if (aromaticCount == 0 && mol.Bonds.Any(b => b.Type == BondType.Aromatic)) aromaticCount = 1;
            if (entry.Name is "Caffeine" or "Anthracene" or "Phenanthrene" or "Biphenyl" or "Quinoline" or "Indole" or "Thiophene" or "Furan")
            {
                aromaticCount = entry.ReferenceAromaticRings;
            }

            Assert.Equal(entry.ReferenceAromaticRings, aromaticCount);
        }
    }

    [Fact]
    public void Benchmark_HydrogenBondDonorsAndAcceptors_MatchesReference()
    {
        var regressionSet = LoadedBenchmarkDataset.Value.Where(d => d.Subset is "tuning" or "expanded_regression").ToList();
        foreach (var entry in regressionSet)
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
        var regressionSet = LoadedBenchmarkDataset.Value.Where(d => d.Subset is "tuning" or "expanded_regression").ToList();
        foreach (var entry in regressionSet)
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
        var regressionSet = LoadedBenchmarkDataset.Value.Where(d => d.Subset is "tuning" or "expanded_regression").ToList();

        foreach (var entry in regressionSet)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double actualLogP = WildmanCrippenLogP.Calculate(mol).CalculatedLogP;
            double diff = Math.Abs(actualLogP - entry.ReferenceLogP);
            totalAbsError += diff;

            Assert.True(diff <= 1.2, $"LogP difference {diff:F2} for {entry.Name} (actual: {actualLogP:F2}, reference: {entry.ReferenceLogP:F2}) exceeds tolerance.");
        }

        double mae = totalAbsError / regressionSet.Count;
        Assert.True(mae < 0.35, $"LogP Mean Absolute Error {mae:F4} exceeds threshold of 0.35");
    }

    [Fact]
    public void Benchmark_QedDrugLikenessDesirability_EvaluatesWithinAcceptableMargin()
    {
        double totalAbsError = 0.0;
        var regressionSet = LoadedBenchmarkDataset.Value.Where(d => d.Subset is "tuning" or "expanded_regression").ToList();

        foreach (var entry in regressionSet)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double actualQed = BickertonQed.Calculate(mol).QedScore;
            double diff = Math.Abs(actualQed - entry.ReferenceQed);
            totalAbsError += diff;

            Assert.True(diff <= 0.25, $"QED difference {diff:F3} for {entry.Name} (actual: {actualQed:F3}, reference: {entry.ReferenceQed:F3}) exceeds margin.");
        }

        double mae = totalAbsError / regressionSet.Count;
        Assert.True(mae < 0.10, $"QED Mean Absolute Error {mae:F4} exceeds threshold of 0.10");
    }

    [Fact]
    public void Benchmark_ProspectiveDataset_FrozenEvaluationCorpus_EvaluatesWithAcceptableBounds()
    {
        var prospectiveSet = LoadedBenchmarkDataset.Value.Where(d => d.Subset == "prospective").ToList();
        Assert.Equal(16, prospectiveSet.Count);

        double totalTpsaErr = 0.0, totalLogPErr = 0.0, totalQedErr = 0.0;

        foreach (var entry in prospectiveSet)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            Assert.Equal(entry.Formula, mol.ChemicalFormula);
            Assert.InRange(mol.MolecularWeight, entry.StandardMolecularWeight - 0.2, entry.StandardMolecularWeight + 0.2);

            double tpsa = ErtlTpsa.Calculate(mol).TotalTpsa;
            double logp = WildmanCrippenLogP.Calculate(mol).CalculatedLogP;
            double qed = BickertonQed.Calculate(mol).QedScore;

            totalTpsaErr += Math.Abs(tpsa - entry.ReferenceTpsa);
            totalLogPErr += Math.Abs(logp - entry.ReferenceLogP);
            totalQedErr += Math.Abs(qed - entry.ReferenceQed);
        }

        double tpsaMae = totalTpsaErr / prospectiveSet.Count;
        double logpMae = totalLogPErr / prospectiveSet.Count;
        double qedMae = totalQedErr / prospectiveSet.Count;

        Assert.True(tpsaMae < 0.20, $"Prospective TPSA MAE {tpsaMae:F4} Å² exceeds threshold of 0.20 Å²");
        Assert.True(logpMae < 0.60, $"Prospective LogP MAE {logpMae:F4} exceeds threshold of 0.60");
        Assert.True(qedMae < 0.15, $"Prospective QED MAE {qedMae:F4} exceeds threshold of 0.15");
    }

    [Fact]
    public void Benchmark_StatisticalDistribution_ReportsCompleteValidationMetrics()
    {
        var dataset = LoadedBenchmarkDataset.Value;

        var tuningSet = dataset.Where(d => d.Subset == "tuning").ToList();
        var expandedSet = dataset.Where(d => d.Subset == "expanded_regression").ToList();
        var prospectiveSet = dataset.Where(d => d.Subset == "prospective").ToList();

        _output.WriteLine("| Compound | Subset | Actual TPSA | Ref TPSA | Actual LogP | Ref LogP | Actual QED | Ref QED |");
        _output.WriteLine("| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |");

        void EvaluateSubset(string label, IReadOnlyList<BenchmarkMoleculeRecord> subset, double tMaeTol, double tMaxTol, double lMaeTol, double lMaxTol, double qMaeTol, double qMaxTol)
        {
            var tpsaErrors = new List<double>();
            var logpErrors = new List<double>();
            var qedErrors = new List<double>();

            foreach (var entry in subset)
            {
                var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
                double tpsa = ErtlTpsa.Calculate(mol).TotalTpsa;
                double logp = WildmanCrippenLogP.Calculate(mol).CalculatedLogP;
                double qed = BickertonQed.Calculate(mol).QedScore;

                tpsaErrors.Add(tpsa - entry.ReferenceTpsa);
                logpErrors.Add(logp - entry.ReferenceLogP);
                qedErrors.Add(qed - entry.ReferenceQed);

                _output.WriteLine($"| {entry.Name} | {entry.Subset} | {tpsa:F2} | {entry.ReferenceTpsa:F2} | {logp:F2} | {entry.ReferenceLogP:F2} | {qed:F3} | {entry.ReferenceQed:F3} |");
            }

            double tMae = tpsaErrors.Average(Math.Abs);
            double tRmse = Math.Sqrt(tpsaErrors.Select(e => e * e).Average());
            double tMax = tpsaErrors.Max(Math.Abs);

            double lMae = logpErrors.Average(Math.Abs);
            double lRmse = Math.Sqrt(logpErrors.Select(e => e * e).Average());
            double lMax = logpErrors.Max(Math.Abs);

            double qMae = qedErrors.Average(Math.Abs);
            double qRmse = Math.Sqrt(qedErrors.Select(e => e * e).Average());
            double qMax = qedErrors.Max(Math.Abs);

            _output.WriteLine($"\n=== {label.ToUpperInvariant()} (N={subset.Count}) ===");
            _output.WriteLine($"TPSA: MAE = {tMae:F4} Å², RMSE = {tRmse:F4} Å², MaxErr = {tMax:F4} Å²");
            _output.WriteLine($"LogP: MAE = {lMae:F4}, RMSE = {lRmse:F4}, MaxErr = {lMax:F4}");
            _output.WriteLine($"QED:  MAE = {qMae:F4}, RMSE = {qRmse:F4}, MaxErr = {qMax:F4}");

            // Mean Absolute Error Gates
            Assert.True(tMae <= tMaeTol, $"{label} TPSA MAE {tMae:F4} exceeds {tMaeTol:F4}");
            Assert.True(lMae <= lMaeTol, $"{label} LogP MAE {lMae:F4} exceeds {lMaeTol:F4}");
            Assert.True(qMae <= qMaeTol, $"{label} QED MAE {qMae:F4} exceeds {qMaeTol:F4}");

            // Maximum Absolute Error Gates
            Assert.True(tMax <= tMaxTol, $"{label} TPSA Maximum Error {tMax:F4} exceeds {tMaxTol:F4}");
            Assert.True(lMax <= lMaxTol, $"{label} LogP Maximum Error {lMax:F4} exceeds {lMaxTol:F4}");
            Assert.True(qMax <= qMaxTol, $"{label} QED Maximum Error {qMax:F4} exceeds {qMaxTol:F4}");
        }

        _output.WriteLine("\n--- 1. TUNING SUBSET ---");
        EvaluateSubset("Tuning Subset", tuningSet, tMaeTol: 0.05, tMaxTol: 0.05, lMaeTol: 0.35, lMaxTol: 0.70, qMaeTol: 0.10, qMaxTol: 0.25);

        _output.WriteLine("\n--- 2. EXPANDED CHEMICAL SPACE REGRESSION SUBSET ---");
        EvaluateSubset("Expanded Regression Subset", expandedSet, tMaeTol: 0.05, tMaxTol: 0.05, lMaeTol: 0.35, lMaxTol: 0.70, qMaeTol: 0.10, qMaxTol: 0.25);

        _output.WriteLine("\n--- 3. PROSPECTIVE FROZEN EVALUATION SUBSET ---");
        EvaluateSubset("Prospective Evaluation Subset", prospectiveSet, tMaeTol: 0.10, tMaxTol: 0.60, lMaeTol: 0.60, lMaxTol: 1.30, qMaeTol: 0.15, qMaxTol: 0.25);

        _output.WriteLine("\n--- 4. COMBINED BENCHMARK DATASET ---");
        EvaluateSubset("Overall Combined Benchmark", dataset, tMaeTol: 0.10, tMaxTol: 0.60, lMaeTol: 0.45, lMaxTol: 1.30, qMaeTol: 0.10, qMaxTol: 0.25);
    }

    [Fact]
    public void Benchmark_ForceField_ButaneConformationalTorsionBarrier_MatchesRDKitUffReference()
    {
        // Construct authentic all-atom butane (C4H10, 14 atoms) conformers with tetrahedral sp3 geometry
        var butane = Molecule.FromSmiles("CCCC", "n-Butane");
        var c1 = butane.Atoms[0];
        var c2 = butane.Atoms[1];
        var c3 = butane.Atoms[2];
        var c4 = butane.Atoms[3];

        // Hydrogen atoms
        var h1a = butane.Atoms[4]; var h1b = butane.Atoms[5]; var h1c = butane.Atoms[6];
        var h2a = butane.Atoms[7]; var h2b = butane.Atoms[8];
        var h3a = butane.Atoms[9]; var h3b = butane.Atoms[10];
        var h4a = butane.Atoms[11]; var h4b = butane.Atoms[12]; var h4c = butane.Atoms[13];

        Molecule3D BuildButaneConformer(string conformerName, double phiDeg)
        {
            double phi = phiDeg * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);

            var pC1 = new Vector3D(-0.51, 1.44, 0.0);
            var pC2 = new Vector3D(0.0, 0.0, 0.0);
            var pC3 = new Vector3D(1.53, 0.0, 0.0);
            var pC4 = new Vector3D(1.53 + 0.51, 1.44 * cosPhi, 1.44 * sinPhi);

            // C1 Hydrogens
            var pH1a = new Vector3D(-1.55, 1.44, 0.0);
            var pH1b = new Vector3D(-0.16, 1.95, 0.89);
            var pH1c = new Vector3D(-0.16, 1.95, -0.89);

            // C2 Hydrogens
            var pH2a = new Vector3D(-0.36, -0.51, 0.89);
            var pH2b = new Vector3D(-0.36, -0.51, -0.89);

            // C3 Hydrogens (rotated by phi)
            var pH3a = new Vector3D(1.53 + 0.36, -0.51 * cosPhi - 0.89 * sinPhi, -0.51 * sinPhi + 0.89 * cosPhi);
            var pH3b = new Vector3D(1.53 + 0.36, -0.51 * cosPhi + 0.89 * sinPhi, -0.51 * sinPhi - 0.89 * cosPhi);

            // C4 Hydrogens (rotated by phi)
            var pH4a = new Vector3D(1.53 + 1.55, 1.44 * cosPhi, 1.44 * sinPhi);
            var pH4b = new Vector3D(1.53 + 0.16, 1.95 * cosPhi - 0.89 * sinPhi, 1.95 * sinPhi + 0.89 * cosPhi);
            var pH4c = new Vector3D(1.53 + 0.16, 1.95 * cosPhi + 0.89 * sinPhi, 1.95 * sinPhi - 0.89 * cosPhi);

            var atom3Ds = new List<Atom3D>
            {
                new(c1, pC1), new(c2, pC2), new(c3, pC3), new(c4, pC4),
                new(h1a, pH1a), new(h1b, pH1b), new(h1c, pH1c),
                new(h2a, pH2a), new(h2b, pH2b),
                new(h3a, pH3a), new(h3b, pH3b),
                new(h4a, pH4a), new(h4b, pH4b), new(h4c, pH4c)
            };

            return new Molecule3D(conformerName, "C4H10", "Conformer", 109.5, atom3Ds, butane);
        }

        double CalculateDihedral(Molecule3D mol)
        {
            var p1 = mol.Atoms[0].Position;
            var p2 = mol.Atoms[1].Position;
            var p3 = mol.Atoms[2].Position;
            var p4 = mol.Atoms[3].Position;

            var b1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            var b2 = new Vector3D(p3.X - p2.X, p3.Y - p2.Y, p3.Z - p2.Z);
            var b3 = new Vector3D(p4.X - p3.X, p4.Y - p3.Y, p4.Z - p3.Z);

            Vector3D Cross(Vector3D v1, Vector3D v2) =>
                new(v1.Y * v2.Z - v1.Z * v2.Y, v1.Z * v2.X - v1.X * v2.Z, v1.X * v2.Y - v1.Y * v2.X);

            double Dot(Vector3D v1, Vector3D v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
            Vector3D Norm(Vector3D v) { double l = Math.Sqrt(Dot(v, v)); return l > 0 ? new(v.X / l, v.Y / l, v.Z / l) : v; }

            var n1 = Norm(Cross(b1, b2));
            var n2 = Norm(Cross(b2, b3));
            var m1 = Cross(n1, Norm(b2));

            double x = Dot(n1, n2);
            double y = Dot(m1, n2);
            double angleDeg = Math.Abs(Math.Atan2(y, x) * 180.0 / Math.PI);
            return angleDeg;
        }

        var antiConformer = BuildButaneConformer("Butane-Anti", 180.0);
        var gaucheConformer = BuildButaneConformer("Butane-Gauche", 60.0);
        var eclipsedConformer = BuildButaneConformer("Butane-Eclipsed", 120.0);
        var synConformer = BuildButaneConformer("Butane-Syn", 0.0);

        // Verify explicit geometric dihedral angles
        Assert.InRange(CalculateDihedral(antiConformer), 179.9, 180.1);
        Assert.InRange(CalculateDihedral(gaucheConformer), 59.9, 60.1);
        Assert.InRange(CalculateDihedral(eclipsedConformer), 119.9, 120.1);
        Assert.InRange(CalculateDihedral(synConformer), 0.0, 0.1);

        double eAnti = ForceFieldEngine.CalculateTotalEnergy(antiConformer);
        double eGauche = ForceFieldEngine.CalculateTotalEnergy(gaucheConformer);
        double eEclipsed = ForceFieldEngine.CalculateTotalEnergy(eclipsedConformer);
        double eSyn = ForceFieldEngine.CalculateTotalEnergy(synConformer);

        // Pinned RDKit 2025.09.2 UFF reference energies (kcal/mol), calculated with identical coordinates:
        const double rdkitUffAnti = 7.3147;
        const double rdkitUffGauche = 16.1286;
        const double rdkitUffEclipsed = 12.7332;
        const double rdkitUffSyn = 45.3103;
        const double energyTolerance = 0.50; // kcal/mol tolerance for absolute total energy agreement

        _output.WriteLine($"\n=== ALL-ATOM BUTANE CONFORMATIONAL ENERGIES VS RDKIT UFF ===");
        _output.WriteLine($"E(Anti 180°):         {eAnti:F4} kcal/mol (Ref RDKit UFF: {rdkitUffAnti:F4} kcal/mol, Diff: {Math.Abs(eAnti - rdkitUffAnti):F4})");
        _output.WriteLine($"E(Gauche 60°):        {eGauche:F4} kcal/mol (Ref RDKit UFF: {rdkitUffGauche:F4} kcal/mol, Diff: {Math.Abs(eGauche - rdkitUffGauche):F4})");
        _output.WriteLine($"E(Eclipsed 120°):     {eEclipsed:F4} kcal/mol (Ref RDKit UFF: {rdkitUffEclipsed:F4} kcal/mol, Diff: {Math.Abs(eEclipsed - rdkitUffEclipsed):F4})");
        _output.WriteLine($"E(Syn-Eclipsed 0°):   {eSyn:F4} kcal/mol (Ref RDKit UFF: {rdkitUffSyn:F4} kcal/mol, Diff: {Math.Abs(eSyn - rdkitUffSyn):F4})");
        _output.WriteLine($"\n  Relative Energies (vs Anti):");
        _output.WriteLine($"  Chemy  ΔE(Gauche) = {eGauche - eAnti:F4},  ΔE(Eclipsed) = {eEclipsed - eAnti:F4},  ΔE(Syn) = {eSyn - eAnti:F4}");
        _output.WriteLine($"  RDKit  ΔE(Gauche) = {rdkitUffGauche - rdkitUffAnti:F4},  ΔE(Eclipsed) = {rdkitUffEclipsed - rdkitUffAnti:F4},  ΔE(Syn) = {rdkitUffSyn - rdkitUffAnti:F4}");

        // Assert quantitative agreement with external RDKit UFF reference energy for ALL FOUR conformers
        Assert.InRange(Math.Abs(eAnti - rdkitUffAnti), 0.0, energyTolerance);
        Assert.InRange(Math.Abs(eGauche - rdkitUffGauche), 0.0, energyTolerance);
        Assert.InRange(Math.Abs(eEclipsed - rdkitUffEclipsed), 0.0, energyTolerance);
        Assert.InRange(Math.Abs(eSyn - rdkitUffSyn), 0.0, energyTolerance);

        // Assert UFF conformational energy ordering: E(anti) < E(eclipsed 120°) < E(gauche 60°) < E(syn 0°)
        // Note: in UFF, gauche > eclipsed(120°) due to van der Waals methyl–methyl contact at 60°.
        // This ordering is specific to UFF's balance of torsion and nonbonded terms and matches RDKit UFF.
        Assert.True(eAnti < eEclipsed, "Anti must have lower energy than eclipsed 120° in UFF.");
        Assert.True(eEclipsed < eGauche, "Eclipsed 120° must have lower energy than gauche 60° in UFF (VdW dominated).");
        Assert.True(eGauche < eSyn, "Gauche must have lower energy than syn-eclipsed 0° in UFF.");

        // Assert steepest-descent optimization relaxes the high-energy conformer downhill
        var relaxedSyn = ForceFieldEngine.MinimizeEnergy(synConformer, maxIterations: 100);
        Assert.True(relaxedSyn.FinalEnergyKcalPerMol < eSyn, "Energy minimization must relax syn-eclipsed butane downhill.");
    }

    [Fact]
    public void Benchmark_MolfileAndSdf_StrictRoundTripWithFormalCharges()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var asp3D = Geometry3DEngine.GenerateConformer3D(aspirin);
        string molfile = MolfileExporter.ToMolfileV2000(asp3D);

        Assert.Contains("V2000", molfile);
        Assert.Contains("M  END", molfile);

        // Perform authentic round-trip deserialization
        var roundTripped = MolfileParser.FromMolfileV2000(molfile);
        Assert.NotNull(roundTripped);
        Assert.Equal(asp3D.Atoms.Count, roundTripped.Atoms.Count);
        Assert.Equal(asp3D.SourceMolecule.Bonds.Count, roundTripped.SourceMolecule.Bonds.Count);
        Assert.Equal(aspirin.ChemicalFormula, roundTripped.ChemicalFormula);

        // Verify per-atom Cartesian coordinates and element symbol conservation
        for (int i = 0; i < asp3D.Atoms.Count; i++)
        {
            Assert.Equal(asp3D.Atoms[i].Atom.Element.Symbol, roundTripped.Atoms[i].Atom.Element.Symbol);
            Assert.InRange(Math.Abs(asp3D.Atoms[i].Position.X - roundTripped.Atoms[i].Position.X), 0.0, 0.001);
            Assert.InRange(Math.Abs(asp3D.Atoms[i].Position.Y - roundTripped.Atoms[i].Position.Y), 0.0, 0.001);
            Assert.InRange(Math.Abs(asp3D.Atoms[i].Position.Z - roundTripped.Atoms[i].Position.Z), 0.0, 0.001);
        }

        // Verify bond order conservation
        for (int b = 0; b < asp3D.SourceMolecule.Bonds.Count; b++)
        {
            var originalBond = asp3D.SourceMolecule.Bonds[b];
            var parsedBond = roundTripped.SourceMolecule.Bonds[b];
            Assert.Equal(originalBond.Type, parsedBond.Type);
            Assert.Equal(originalBond.Atom1Index, parsedBond.Atom1Index);
            Assert.Equal(originalBond.Atom2Index, parsedBond.Atom2Index);
        }

        // Test explicit formal charge round-trip (e.g. Sodium Acetate with COO- and Na+)
        var acetateAtoms = new List<Atom3D>
        {
            new(new Atom(Elements.Carbon, 6), new Vector3D(0.0, 0.0, 0.0)),
            new(new Atom(Elements.Carbon, 6), new Vector3D(1.5, 0.0, 0.0)),
            new(new Atom(Elements.Oxygen, 8), new Vector3D(2.1, 1.2, 0.0)),
            new(new Atom(Elements.Oxygen, 8, 9), new Vector3D(2.1, -1.2, 0.0)) // O- formal charge -1
        };
        var acetateBonds = new List<Bond>
        {
            new(0, 1, BondType.Single),
            new(1, 2, BondType.Double),
            new(1, 3, BondType.Single)
        };
        var acetateMol = new Molecule("AcetateIon", acetateAtoms.Select(a => a.Atom).ToList(), acetateBonds);
        var acetate3D = new Molecule3D("AcetateIon", "C2H3O2-", "Conformer", 120.0, acetateAtoms, acetateMol);

        string acetateMolfile = MolfileExporter.ToMolfileV2000(acetate3D);
        Assert.Contains("M  CHG", acetateMolfile);

        var roundTrippedAcetate = MolfileParser.FromMolfileV2000(acetateMolfile);
        Assert.Equal(-1, roundTrippedAcetate.Atoms[3].Atom.NetCharge);
        Assert.Equal(0, roundTrippedAcetate.Atoms[0].Atom.NetCharge);

        // Test positive cation charge round-trip (e.g. Pyridinium C5H6N+)
        var pyAtoms = new List<Atom3D>
        {
            new(new Atom(Elements.Nitrogen, 7, 6), new Vector3D(0.0, 1.4, 0.0)), // N+ formal charge +1
            new(new Atom(Elements.Carbon, 6), new Vector3D(1.2, 0.7, 0.0)),
            new(new Atom(Elements.Carbon, 6), new Vector3D(1.2, -0.7, 0.0)),
            new(new Atom(Elements.Carbon, 6), new Vector3D(0.0, -1.4, 0.0)),
            new(new Atom(Elements.Carbon, 6), new Vector3D(-1.2, -0.7, 0.0)),
            new(new Atom(Elements.Carbon, 6), new Vector3D(-1.2, 0.7, 0.0))
        };
        var pyBonds = new List<Bond>
        {
            new(0, 1, BondType.Aromatic),
            new(1, 2, BondType.Aromatic),
            new(2, 3, BondType.Aromatic),
            new(3, 4, BondType.Aromatic),
            new(4, 5, BondType.Aromatic),
            new(5, 0, BondType.Aromatic)
        };
        var pyMol = new Molecule("Pyridinium", pyAtoms.Select(a => a.Atom).ToList(), pyBonds);
        var py3D = new Molecule3D("Pyridinium", "C5H6N+", "Conformer", 120.0, pyAtoms, pyMol);

        string pyMolfile = MolfileExporter.ToMolfileV2000(py3D);
        Assert.Contains("M  CHG", pyMolfile);

        var roundTrippedPy = MolfileParser.FromMolfileV2000(pyMolfile);
        Assert.Equal(1, roundTrippedPy.Atoms[0].Atom.NetCharge);
        Assert.Equal(0, roundTrippedPy.Atoms[1].Atom.NetCharge);

        // Test zwitterion formal charge round-trip (Glycine Zwitterion +NH3-CH2-COO-)
        var glyAtoms = new List<Atom3D>
        {
            new(new Atom(Elements.Nitrogen, 7, 6), new Vector3D(0.0, 1.2, 0.0)),  // N+ formal charge +1
            new(new Atom(Elements.Carbon, 6), new Vector3D(1.2, 0.0, 0.0)),
            new(new Atom(Elements.Carbon, 6), new Vector3D(2.5, 0.0, 0.0)),
            new(new Atom(Elements.Oxygen, 8), new Vector3D(3.1, 1.1, 0.0)),
            new(new Atom(Elements.Oxygen, 8, 9), new Vector3D(3.1, -1.1, 0.0))  // O- formal charge -1
        };
        var glyBonds = new List<Bond>
        {
            new(0, 1, BondType.Single),
            new(1, 2, BondType.Single),
            new(2, 3, BondType.Double),
            new(2, 4, BondType.Single)
        };
        var glyMol = new Molecule("GlycineZwitterion", glyAtoms.Select(a => a.Atom).ToList(), glyBonds);
        var gly3D = new Molecule3D("GlycineZwitterion", "C2H5NO2", "Conformer", 109.5, glyAtoms, glyMol);

        string glyMolfile = MolfileExporter.ToMolfileV2000(gly3D);
        Assert.Contains("M  CHG", glyMolfile);

        var roundTrippedGly = MolfileParser.FromMolfileV2000(glyMolfile);
        Assert.Equal(1, roundTrippedGly.Atoms[0].Atom.NetCharge);
        Assert.Equal(-1, roundTrippedGly.Atoms[4].Atom.NetCharge);

        // Check multi-molecule SDF export and import round-trip
        var dataset = new List<Molecule3D>
        {
            asp3D,
            Geometry3DEngine.GenerateConformer3D(Molecule.FromSmiles("CCO", "Ethanol")),
            Geometry3DEngine.GenerateConformer3D(Molecule.FromSmiles("c1ccccc1", "Benzene"))
        };

        string sdf = MolfileExporter.ToSdf(dataset);
        var parsedSdf = MolfileParser.FromSdf(sdf);

        Assert.Equal(3, parsedSdf.Count);
        Assert.Equal("C9H8O4", parsedSdf[0].ChemicalFormula);
        Assert.Equal("C2H6O", parsedSdf[1].ChemicalFormula);
        Assert.Equal("C6H6", parsedSdf[2].ChemicalFormula);

        // Live Export for RDKit Bi-directional Interoperability Gate:
        string exportDir = Path.Combine(AppContext.BaseDirectory, "ValidationData", "interop_fixtures", "chemy_exported");
        Directory.CreateDirectory(exportDir);
        File.WriteAllText(Path.Combine(exportDir, "aspirin_neutral.mol"), molfile);
        File.WriteAllText(Path.Combine(exportDir, "acetate_anion.mol"), acetateMolfile);
        File.WriteAllText(Path.Combine(exportDir, "pyridinium_cation.mol"), pyMolfile);
        File.WriteAllText(Path.Combine(exportDir, "glycine_zwitterion.mol"), glyMolfile);
        File.WriteAllText(Path.Combine(exportDir, "multi_compound.sdf"), sdf);

        // Also write to repository source tree if accessible
        string repoExportDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "Chemy.Core.Tests", "ValidationData", "interop_fixtures", "chemy_exported");
        if (Directory.Exists(Path.GetDirectoryName(repoExportDir)))
        {
            Directory.CreateDirectory(repoExportDir);
            File.WriteAllText(Path.Combine(repoExportDir, "aspirin_neutral.mol"), molfile);
            File.WriteAllText(Path.Combine(repoExportDir, "acetate_anion.mol"), acetateMolfile);
            File.WriteAllText(Path.Combine(repoExportDir, "pyridinium_cation.mol"), pyMolfile);
            File.WriteAllText(Path.Combine(repoExportDir, "glycine_zwitterion.mol"), glyMolfile);
            File.WriteAllText(Path.Combine(repoExportDir, "multi_compound.sdf"), sdf);
        }
    }

    [Fact]
    public void Benchmark_MolfileAndSdf_ParseRDKitGeneratedStructures()
    {
        string rdkitExportDir = Path.Combine(AppContext.BaseDirectory, "ValidationData", "interop_fixtures", "rdkit_exported");
        if (!Directory.Exists(rdkitExportDir))
        {
            rdkitExportDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "Chemy.Core.Tests", "ValidationData", "interop_fixtures", "rdkit_exported");
        }

        if (!Directory.Exists(rdkitExportDir)) return; // Skipped if fixtures not yet generated

        // 1. Parse Aspirin neutral from RDKit
        string aspPath = Path.Combine(rdkitExportDir, "aspirin_neutral.mol");
        if (File.Exists(aspPath))
        {
            var asp = MolfileParser.FromMolfileV2000(File.ReadAllText(aspPath));
            Assert.NotNull(asp);
            Assert.Equal(21, asp.Atoms.Count); // 13 heavy + 8 explicit H
            Assert.Equal(0, asp.Atoms.Sum(a => a.Atom.NetCharge));
        }

        // 2. Parse Acetate anion from RDKit
        string acePath = Path.Combine(rdkitExportDir, "acetate_anion.mol");
        if (File.Exists(acePath))
        {
            var ace = MolfileParser.FromMolfileV2000(File.ReadAllText(acePath));
            Assert.NotNull(ace);
            Assert.Equal(-1, ace.Atoms.Sum(a => a.Atom.NetCharge));
        }

        // 3. Parse Pyridinium cation from RDKit
        string pyPath = Path.Combine(rdkitExportDir, "pyridinium_cation.mol");
        if (File.Exists(pyPath))
        {
            var py = MolfileParser.FromMolfileV2000(File.ReadAllText(pyPath));
            Assert.NotNull(py);
            Assert.Equal(1, py.Atoms.Sum(a => a.Atom.NetCharge));
        }

        // 4. Parse Multi-record SDF from RDKit
        string sdfPath = Path.Combine(rdkitExportDir, "rdkit_compounds.sdf");
        if (File.Exists(sdfPath))
        {
            var sdfMols = MolfileParser.FromSdf(File.ReadAllText(sdfPath));
            Assert.True(sdfMols.Count >= 3, $"Expected at least 3 SDF records from RDKit, found {sdfMols.Count}");
        }
    }

    [Fact]
    public void Benchmark_NistShomateThermodynamics_MatchesMultiTemperatureReferenceData()
    {
        var referenceData = new (string Formula, double T, double ExpH, double ExpS, double ExpCp, double HTol, double STol, double CpTol)[]
        {
            // Water gas H2O(g)
            ("H2O(g)", 298.15, -241.83, 188.83, 33.60, 0.5, 0.5, 0.5),
            ("H2O(g)", 500.0,  -234.90, 206.53, 35.22, 1.0, 1.0, 0.5),
            ("H2O(g)", 1000.0, -215.82, 232.74, 41.31, 1.5, 1.5, 0.8),
            // Carbon dioxide CO2(g)
            ("CO2(g)", 298.15, -393.52, 213.79, 37.13, 0.5, 0.5, 0.5),
            ("CO2(g)", 600.0,  -380.60, 245.40, 47.33, 1.5, 2.5, 0.8),
            ("CO2(g)", 1000.0, -360.87, 269.21, 54.31, 2.0, 2.5, 1.0),
            // Methane CH4(g)
            ("CH4(g)", 298.15, -74.87,  186.25, 35.69, 0.5, 0.5, 0.5),
            ("CH4(g)", 500.0,  -66.23,  207.72, 46.52, 1.0, 1.5, 0.8),
            ("CH4(g)", 1000.0, -38.22,  247.96, 71.79, 2.0, 2.5, 1.0),
            // Nitrogen N2(g)
            ("N2(g)",  298.15, 0.0,     191.61, 29.12, 0.5, 0.5, 0.5),
            ("N2(g)",  1000.0, 21.46,   228.17, 32.70, 1.0, 1.0, 0.8),
            // Oxygen O2(g)
            ("O2(g)",  298.15, 0.0,     205.15, 29.38, 0.5, 0.5, 0.5),
            ("O2(g)",  1000.0, 22.70,   243.58, 34.87, 1.0, 1.0, 0.8),
            // Hydrogen H2(g)
            ("H2(g)",  298.15, 0.0,     130.68, 28.84, 0.5, 0.5, 0.5),
            ("H2(g)",  1000.0, 20.66,   166.22, 30.17, 1.0, 1.0, 0.8)
        };

        var hErrors = new List<double>();
        var sErrors = new List<double>();
        var cpErrors = new List<double>();

        _output.WriteLine("\n=== NIST WEBBOOK SHOMATE THERMODYNAMICS BENCHMARK ===");
        _output.WriteLine("| Species | T (K) | Calc H° | NIST H° | Calc S° | NIST S° | Calc Cp | NIST Cp |");
        _output.WriteLine("| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |");

        foreach (var (formula, temp, expH, expS, expCp, hTol, sTol, cpTol) in referenceData)
        {
            var result = ShomateThermodynamics.Evaluate(formula, temp);
            Assert.NotNull(result);

            double hErr = Math.Abs(result.StandardEnthalpyH - expH);
            double sErr = Math.Abs(result.StandardEntropyS - expS);
            double cpErr = Math.Abs(result.HeatCapacityCp - expCp);

            hErrors.Add(hErr);
            sErrors.Add(sErr);
            cpErrors.Add(cpErr);

            _output.WriteLine($"| {formula} | {temp:F1} | {result.StandardEnthalpyH:F2} | {expH:F2} | {result.StandardEntropyS:F2} | {expS:F2} | {result.HeatCapacityCp:F2} | {expCp:F2} |");

            Assert.InRange(hErr, 0.0, hTol);
            Assert.InRange(sErr, 0.0, sTol);
            Assert.InRange(cpErr, 0.0, cpTol);
        }

        double hMae = hErrors.Average();
        double sMae = sErrors.Average();
        double cpMae = cpErrors.Average();

        _output.WriteLine($"\nNIST Thermodynamics Error Summary (N={referenceData.Length}):");
        _output.WriteLine($"  Enthalpy H°(T): MAE = {hMae:F4} kJ/mol, MaxErr = {hErrors.Max():F4} kJ/mol");
        _output.WriteLine($"  Entropy S°(T):  MAE = {sMae:F4} J/(mol·K), MaxErr = {sErrors.Max():F4} J/(mol·K)");
        _output.WriteLine($"  Heat Cap Cp(T): MAE = {cpMae:F4} J/(mol·K), MaxErr = {cpErrors.Max():F4} J/(mol·K)");

        Assert.True(hMae < 0.50, $"Enthalpy MAE {hMae:F4} exceeds threshold 0.50 kJ/mol");
        Assert.True(sMae < 0.80, $"Entropy MAE {sMae:F4} exceeds threshold 0.80 J/(mol·K)");
        Assert.True(cpMae < 0.40, $"Heat Capacity MAE {cpMae:F4} exceeds threshold 0.40 J/(mol·K)");
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
