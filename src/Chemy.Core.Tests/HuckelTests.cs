using System;
using System.Linq;
using Chemy.Core;
using Chemy.Core.Quantum;
using Xunit;

namespace Chemy.Core.Tests;

public class HuckelTests
{
    [Fact]
    public void JacobiEigensolver_DiagonalizesKnownSymmetricMatrix()
    {
        // 2x2 symmetric matrix: [2, 1; 1, 2] -> Eigenvalues are 3 and 1
        double[,] matrix = {
            { 2.0, 1.0 },
            { 1.0, 2.0 }
        };

        var (eigenvalues, eigenvectors) = JacobiEigensolver.Diagonalize(matrix);

        Assert.Equal(2, eigenvalues.Length);
        var sorted = eigenvalues.OrderByDescending(x => x).ToArray();
        Assert.Equal(3.0, sorted[0], precision: 5);
        Assert.Equal(1.0, sorted[1], precision: 5);
    }

    [Fact]
    public void Huckel_Ethylene_MatchesAnalyticalSolution()
    {
        var ethylene = Molecule.FromSmiles("C=C", "Ethylene");
        var result = HuckelEngine.Analyze(ethylene);

        Assert.NotNull(result);
        Assert.Equal(2, result.ConjugatedAtomCount);
        Assert.Equal(2, result.TotalPiElectrons);
        Assert.Equal(2, result.Orbitals.Count);

        // Eigenvalues: +1.000β and -1.000β
        Assert.Equal(1.000, result.HomoEnergyBetaCoeff, precision: 3);
        Assert.Equal(-1.000, result.LumoEnergyBetaCoeff, precision: 3);
        Assert.Equal(2.000, result.HomoLumoGapBetaCoeff, precision: 3);

        // Total π energy = 2 * (+1.0) = 2.0β; Resonance energy = 0.0β
        Assert.Equal(2.000, result.TotalPiEnergyBetaCoeff, precision: 3);
        Assert.Equal(0.000, result.DewarResonanceEnergyBetaCoeff, precision: 3);

        // Coulson bond order between C1 and C2 is exactly 1.000
        Assert.Single(result.PiBondOrders);
        Assert.Equal(1.000, result.PiBondOrders[0].BondOrder, precision: 3);
        Assert.Equal(1.337, result.PiBondOrders[0].EstimatedBondLengthAngstrom, precision: 3);
    }

    [Fact]
    public void Huckel_13Butadiene_MatchesAnalyticalEigenvalues()
    {
        var butadiene = Molecule.FromSmiles("C=CC=C", "1,3-Butadiene");
        var result = butadiene.ComputeHuckelOrbitals();

        Assert.NotNull(result);
        Assert.Equal(4, result.ConjugatedAtomCount);
        Assert.Equal(4, result.TotalPiElectrons);

        // Analytical eigenvalues: ±1.618 and ±0.618
        var betaValues = result.Orbitals.Select(o => o.EnergyBetaCoeff).OrderByDescending(x => x).ToList();
        Assert.Equal(1.618, betaValues[0], precision: 3);
        Assert.Equal(0.618, betaValues[1], precision: 3);
        Assert.Equal(-0.618, betaValues[2], precision: 3);
        Assert.Equal(-1.618, betaValues[3], precision: 3);

        // HOMO is orbital 2 (+0.618β), LUMO is orbital 3 (-0.618β)
        Assert.Equal(2, result.HomoIndex);
        Assert.Equal(3, result.LumoIndex);
        Assert.Equal(1.236, result.HomoLumoGapBetaCoeff, precision: 3);

        // Total π-energy = 2*(1.618) + 2*(0.618) = 4.472β
        // Conjugation resonance stabilization = 4.472 - 4.0 = +0.472β
        Assert.Equal(4.472, result.TotalPiEnergyBetaCoeff, precision: 3);
        Assert.Equal(0.472, result.DewarResonanceEnergyBetaCoeff, precision: 3);
    }

    [Fact]
    public void Huckel_Cyclobutadiene_AntiAromaticNonBondingDegeneracy()
    {
        // 4-membered cyclic ring Hamiltonian
        double[,] H = {
            { 0.0, 1.0, 0.0, 1.0 },
            { 1.0, 0.0, 1.0, 0.0 },
            { 0.0, 1.0, 0.0, 1.0 },
            { 1.0, 0.0, 1.0, 0.0 }
        };

        var result = HuckelEngine.AnalyzeMatrix("Cyclobutadiene", H, [1, 1, 1, 1]);

        Assert.NotNull(result);
        Assert.Equal(4, result.ConjugatedAtomCount);
        Assert.Equal(4, result.TotalPiElectrons);

        // Analytical eigenvalues: +2.0, 0.0, 0.0, -2.0
        var betaValues = result.Orbitals.Select(o => o.EnergyBetaCoeff).OrderByDescending(x => x).ToList();
        Assert.Equal(2.000, betaValues[0], precision: 3);
        Assert.Equal(0.000, betaValues[1], precision: 3);
        Assert.Equal(0.000, betaValues[2], precision: 3);
        Assert.Equal(-2.000, betaValues[3], precision: 3);

        // Anti-aromatic: Total π energy = 2*(2.0) + 2*(0.0) = 4.0β -> Resonance energy = 0.0β
        Assert.Equal(4.000, result.TotalPiEnergyBetaCoeff, precision: 3);
        Assert.Equal(0.000, result.DewarResonanceEnergyBetaCoeff, precision: 3);
    }

    [Fact]
    public void Huckel_Benzene_AromaticResonanceEnergyAndBondOrders()
    {
        var benzene = Molecule.FromSmiles("c1ccccc1", "Benzene");
        var result = HuckelEngine.Analyze(benzene);

        Assert.NotNull(result);
        Assert.Equal(6, result.ConjugatedAtomCount);
        Assert.Equal(6, result.TotalPiElectrons);

        // Analytical eigenvalues: +2.000, +1.000, +1.000, -1.000, -1.000, -2.000
        var betaValues = result.Orbitals.Select(o => o.EnergyBetaCoeff).OrderByDescending(x => x).ToList();
        Assert.Equal(2.000, betaValues[0], precision: 3);
        Assert.Equal(1.000, betaValues[1], precision: 3);
        Assert.Equal(1.000, betaValues[2], precision: 3);
        Assert.Equal(-1.000, betaValues[3], precision: 3);
        Assert.Equal(-1.000, betaValues[4], precision: 3);
        Assert.Equal(-2.000, betaValues[5], precision: 3);

        // Total π energy = 2*(2.0) + 4*(1.0) = 8.000β
        // Dewar Aromatic Resonance Energy = 8.000 - 6.000 = +2.000β (~125 kcal/mol in standard scale)
        Assert.Equal(8.000, result.TotalPiEnergyBetaCoeff, precision: 3);
        Assert.Equal(2.000, result.DewarResonanceEnergyBetaCoeff, precision: 3);

        // Coulson Bond Orders are all exactly 2/3 = 0.667
        Assert.Equal(6, result.PiBondOrders.Count);
        foreach (var bo in result.PiBondOrders)
        {
            Assert.Equal(0.667, bo.BondOrder, precision: 3);
            Assert.Equal(1.397, bo.EstimatedBondLengthAngstrom, precision: 3); // Experimental benzene C-C is 1.397 Å
        }

        // Uniform π charges: q_i = 1.000 -> Net charge = 0.000
        foreach (var c in result.AtomCharges)
        {
            Assert.Equal(1.000, c.PiElectronDensity, precision: 3);
            Assert.Equal(0.000, c.NetCharge, precision: 3);
        }
    }

    [Fact]
    public void Huckel_Naphthalene_MatchesAnalyticalPiEnergy()
    {
        // 10-carbon fused bicyclic naphthalene ring adjacency
        double[,] H = new double[10, 10];
        int[,] bonds = {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 5 },
            { 5, 6 }, { 6, 7 }, { 7, 8 }, { 8, 9 }, { 9, 0 },
            { 0, 5 } // Bridge bond
        };

        for (int i = 0; i < bonds.GetLength(0); i++)
        {
            int u = bonds[i, 0];
            int v = bonds[i, 1];
            H[u, v] = 1.0;
            H[v, u] = 1.0;
        }

        var result = HuckelEngine.AnalyzeMatrix("Naphthalene", H, [1, 1, 1, 1, 1, 1, 1, 1, 1, 1]);

        Assert.NotNull(result);
        Assert.Equal(10, result.ConjugatedAtomCount);
        Assert.Equal(10, result.TotalPiElectrons);

        // Analytical total π energy for Naphthalene = 13.683β
        Assert.Equal(13.683, result.TotalPiEnergyBetaCoeff, precision: 2);

        // Dewar resonance stabilization energy = 13.683 - 10.000 = +3.683β
        Assert.Equal(3.683, result.DewarResonanceEnergyBetaCoeff, precision: 2);
    }

    [Fact]
    public void Huckel_Pyridine_HeteroatomPolarization()
    {
        var pyridine = Molecule.FromSmiles("c1ccncc1", "Pyridine");
        var result = HuckelEngine.Analyze(pyridine);

        Assert.NotNull(result);
        Assert.Equal(6, result.ConjugatedAtomCount);
        Assert.Equal(6, result.TotalPiElectrons);

        // Nitrogen is electronegative (h_N = 0.5) and draws π-electron density (q_N > 1.0)
        var nCharge = result.AtomCharges.FirstOrDefault(c => c.Symbol == "N");
        Assert.NotNull(nCharge);
        Assert.True(nCharge.PiElectronDensity > 1.05, $"Expected q_N > 1.05, but got {nCharge.PiElectronDensity}");
        Assert.True(nCharge.NetCharge < -0.05, $"Expected negative net charge on N, but got {nCharge.NetCharge}");
    }

    [Fact]
    public void Huckel_Anthracene_MatchesAnalyticalPiEnergy()
    {
        // 14-carbon linear 3-fused aromatic anthracene rings
        double[,] H = new double[14, 14];
        int[,] bonds = {
            // Ring 1 (6 bonds)
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 5 }, { 5, 0 },
            // Ring 2 (5 bonds, shared with 4-5)
            { 4, 6 }, { 6, 7 }, { 7, 8 }, { 8, 9 }, { 9, 5 },
            // Ring 3 (5 bonds, shared with 7-8)
            { 7, 10 }, { 10, 11 }, { 11, 12 }, { 12, 13 }, { 13, 8 }
        };

        for (int i = 0; i < bonds.GetLength(0); i++)
        {
            int u = bonds[i, 0];
            int v = bonds[i, 1];
            H[u, v] = 1.0;
            H[v, u] = 1.0;
        }

        var result = HuckelEngine.AnalyzeMatrix("Anthracene", H, Enumerable.Repeat(1, 14).ToArray());

        Assert.NotNull(result);
        Assert.Equal(14, result.ConjugatedAtomCount);
        Assert.Equal(14, result.TotalPiElectrons);

        // Analytical total π energy for Anthracene = 19.314β
        Assert.Equal(19.314, result.TotalPiEnergyBetaCoeff, precision: 2);

        // Resonance energy = 19.314 - 14.000 = +5.314β
        Assert.Equal(5.314, result.DewarResonanceEnergyBetaCoeff, precision: 2);
    }

    [Fact]
    public void Huckel_NonConjugatedMolecule_ThrowsInvalidOperationException()
    {
        var methane = Molecule.FromSmiles("C", "Methane");
        Assert.Throws<InvalidOperationException>(() => HuckelEngine.Analyze(methane));
    }
}
