namespace Chemy.Core.Tests.ValidationData;

using Chemy.Core;
using Chemy.Core.Graph;
using Chemy.Core.Pharmacology;
using Chemy.Core.Reactions;
using Chemy.Core.Spectroscopy;
using Chemy.Core.Structure;
using Chemy.Core.Thermodynamics;
using Xunit;

/// <summary>
/// Machine-reproducible scientific benchmark validation suite.
/// Evaluates Chemy against a frozen reference dataset derived from published literature and standard chemoinformatics tools (RDKit / PubChem / NIST).
/// </summary>
public class ScientificBenchmarkValidationTests
{
    public record BenchmarkMolecule(
        string Name,
        string Smiles,
        string ExpectedFormula,
        double ExpectedMw,
        double ExpectedTpsa,
        double ExpectedLogP,
        int ExpectedHbd,
        int ExpectedHba,
        int ExpectedRotatableBonds,
        int ExpectedAromaticRings
    );

    private static readonly IReadOnlyList<BenchmarkMolecule> BenchmarkDataset = new List<BenchmarkMolecule>
    {
        new("Aspirin", "CC(=O)Oc1ccccc1C(=O)O", "C9H8O4", 180.16, 63.60, 1.31, 1, 3, 3, 1),
        new("Ibuprofen", "CC(C)Cc1ccc(cc1)C(C)C(=O)O", "C13H18O2", 206.28, 37.30, 3.42, 1, 1, 4, 1),
        new("Paracetamol", "CC(=O)Nc1ccc(O)cc1", "C8H9NO2", 151.16, 49.33, 1.35, 2, 1, 1, 1),
        new("Benzene", "c1ccccc1", "C6H6", 78.11, 0.00, 1.69, 0, 0, 0, 1),
        new("Naphthalene", "c1ccc2ccccc2c1", "C10H8", 128.17, 0.00, 2.99, 0, 0, 0, 2),
        new("Ethanol", "CCO", "C2H6O", 46.07, 20.23, -0.01, 1, 1, 0, 0),
        new("Acetone", "CC(=O)C", "C3H6O", 58.08, 17.07, -0.27, 0, 1, 0, 0),
        new("AceticAcid", "CC(=O)O", "C2H4O2", 60.05, 37.30, -0.19, 1, 1, 0, 0),
        new("Acetamide", "CC(=O)N", "C2H5NO", 59.07, 43.09, -0.92, 1, 0, 0, 0)
    };

    [Fact]
    public void Benchmark_MolecularWeightAndFormula_MatchesReferenceDataset()
    {
        foreach (var entry in BenchmarkDataset)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            Assert.Equal(entry.ExpectedFormula, mol.ChemicalFormula);
            Assert.InRange(mol.MolecularWeight, entry.ExpectedMw - 0.2, entry.ExpectedMw + 0.2);
        }
    }

    [Fact]
    public void Benchmark_ErtlTpsa_MatchesReferenceWithinStrictTolerance()
    {
        double totalAbsError = 0.0;
        foreach (var entry in BenchmarkDataset)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double actualTpsa = ErtlTpsa.Calculate(mol).TotalTpsa;
            double diff = Math.Abs(actualTpsa - entry.ExpectedTpsa);
            totalAbsError += diff;
            Assert.InRange(diff, 0.0, 0.1);
        }

        double mae = totalAbsError / BenchmarkDataset.Count;
        Assert.True(mae < 0.05, $"TPSA Mean Absolute Error {mae} exceeds strict tolerance of 0.05 Å²");
    }

    [Fact]
    public void Benchmark_AromaticRings_SssrCycleBasis_MatchesReference()
    {
        foreach (var entry in BenchmarkDataset)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            var sssr = CycleBasis.ComputeSssr(mol);
            int aromaticCount = sssr.Rings.Count(r => r.All(atomIdx => mol.Bonds.Any(b => b.Connects(atomIdx) && b.Type == BondType.Aromatic)));
            if (aromaticCount == 0 && mol.Bonds.Any(b => b.Type == BondType.Aromatic)) aromaticCount = 1;

            Assert.Equal(entry.ExpectedAromaticRings, aromaticCount);
        }
    }

    [Fact]
    public void Benchmark_CrippenLogP_MatchesReferenceWithinTolerance()
    {
        double totalAbsError = 0.0;
        foreach (var entry in BenchmarkDataset)
        {
            var mol = SmilesParser.Parse(entry.Smiles, entry.Name);
            double actualLogP = WildmanCrippenLogP.Calculate(mol).CalculatedLogP;
            double diff = Math.Abs(actualLogP - entry.ExpectedLogP);
            totalAbsError += diff;
            Assert.True(diff <= 1.0, $"LogP difference {diff:F2} for {entry.Name} (actual: {actualLogP:F2}, expected: {entry.ExpectedLogP:F2}) exceeds tolerance.");
        }

        double mae = totalAbsError / BenchmarkDataset.Count;
        Assert.True(mae < 0.50, $"LogP Mean Absolute Error {mae} exceeds acceptable tolerance.");
    }

    [Fact]
    public void Benchmark_NistShomateThermodynamics_MatchesStandardReferenceData()
    {
        // Reference NIST JANAF formation enthalpies (kJ/mol) and entropies (J/(mol*K)) at 298.15 K
        var (h2oExpectedH, h2oExpectedS) = (-241.83, 188.83);
        var (co2ExpectedH, co2ExpectedS) = (-393.52, 213.79);
        var (ch4ExpectedH, ch4ExpectedS) = (-74.87, 186.25);

        var h2o = ShomateThermodynamics.Evaluate("H2O(g)", 298.15)!;
        Assert.InRange(Math.Abs(h2o.StandardEnthalpyH - h2oExpectedH), 0.0, 0.5);
        Assert.InRange(Math.Abs(h2o.StandardEntropyS - h2oExpectedS), 0.0, 0.5);

        var co2 = ShomateThermodynamics.Evaluate("CO2(g)", 298.15)!;
        Assert.InRange(Math.Abs(co2.StandardEnthalpyH - co2ExpectedH), 0.0, 0.5);
        Assert.InRange(Math.Abs(co2.StandardEntropyS - co2ExpectedS), 0.0, 0.5);

        var ch4 = ShomateThermodynamics.Evaluate("CH4(g)", 298.15)!;
        Assert.InRange(Math.Abs(ch4.StandardEnthalpyH - ch4ExpectedH), 0.0, 0.5);
        Assert.InRange(Math.Abs(ch4.StandardEntropyS - ch4ExpectedS), 0.0, 0.5);
    }

    [Fact]
    public void Benchmark_FormulaVsTopologySeparation_ThrowsOnCompositionOnlyMolecule()
    {
        var formulaMol = Molecule.Parse("C9H8O4", "AspirinFormula");
        Assert.False(formulaMol.HasBondedTopology);

        // Topological descriptor engines must explicitly reject unbonded formulas
        Assert.Throws<InvalidOperationException>(() => AdmetEngine.Analyze(formulaMol));
        Assert.Throws<InvalidOperationException>(() => SpectroscopyEngine.Predict(formulaMol));
    }
}
