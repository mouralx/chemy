using System.Text;
using Chemy.Core.Graph;

namespace Chemy.Core.Quantum;

/// <summary>
/// Represents a single molecular orbital calculated via Hückel Molecular Orbital (HMO) theory.
/// </summary>
public record MolecularOrbital(
    int OrbitalIndex,
    double EnergyAlphaCoeff,
    double EnergyBetaCoeff,
    double EnergyEv,
    int ElectronOccupancy,
    IReadOnlyList<double> Coefficients,
    string OrbitalType
);

/// <summary>
/// Represents Coulson π-bond order and estimated bond length between two conjugated atoms.
/// </summary>
public record PiBondOrder(
    int Atom1Index,
    string Atom1Symbol,
    int Atom2Index,
    string Atom2Symbol,
    double BondOrder,
    double EstimatedBondLengthAngstrom
);

/// <summary>
/// Represents π-electron density, core charge, and net charge on a conjugated atom.
/// </summary>
public record AtomPiCharge(
    int AtomIndex,
    string Symbol,
    double CoreCharge,
    double PiElectronDensity,
    double NetCharge
);

/// <summary>
/// Represents frontier orbital Fukui reactivity indices (electrophilic, nucleophilic, radical).
/// </summary>
public record FukuiIndex(
    int AtomIndex,
    string Symbol,
    double ElectrophilicAttackIndex, // f- (governed by HOMO)
    double NucleophilicAttackIndex,  // f+ (governed by LUMO)
    double RadicalAttackIndex        // f0 = (f+ + f-) / 2
);

/// <summary>
/// Represents the complete result of a Hückel Molecular Orbital (HMO) quantum electronic structure calculation.
/// </summary>
public record HuckelResult(
    string MoleculeName,
    int ConjugatedAtomCount,
    int TotalPiElectrons,
    IReadOnlyList<MolecularOrbital> Orbitals,
    int HomoIndex,
    int LumoIndex,
    double HomoEnergyBetaCoeff,
    double LumoEnergyBetaCoeff,
    double HomoLumoGapBetaCoeff,
    double HomoLumoGapEv,
    double EstimatedUvVisMaxWavelengthNm,
    double TotalPiEnergyBetaCoeff,
    double LocalizedPiEnergyBetaCoeff,
    double DewarResonanceEnergyBetaCoeff,
    double DewarResonanceEnergyKcalPerMol,
    IReadOnlyList<PiBondOrder> PiBondOrders,
    IReadOnlyList<AtomPiCharge> AtomCharges,
    IReadOnlyList<FukuiIndex> FukuiIndices
);

/// <summary>
/// Exact Hückel Molecular Orbital (HMO) Quantum Engine.
/// Solves the secular equation det|H - E·I| = 0 for conjugated π-systems using the exact Jacobi symmetric eigensolver.
/// Calculates HOMO/LUMO levels, bandgaps, UV-Vis λ_max, Dewar resonance energy, Coulson bond orders, and Fukui indices.
/// </summary>
public static class HuckelEngine
{
    // Standard standard semi-empirical resonance integral |β_0| in eV (~2.71 eV = 62.5 kcal/mol)
    public const double DefaultBetaEv = 2.71;
    public const double DefaultBetaKcalPerMol = 62.5;
    public const double PlanckSpeedOfLightConstantNmEv = 1239.84193; // hc in nm·eV

    /// <summary>
    /// Computes the complete Hückel molecular orbital electronic structure for a given molecule.
    /// </summary>
    public static HuckelResult Analyze(Molecule molecule, double betaEv = DefaultBetaEv)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        // 1. Identify conjugated π-system atoms
        var conjAtoms = IdentifyConjugatedAtoms(molecule);
        if (conjAtoms.Count < 2)
        {
            throw new InvalidOperationException($"Molecule '{molecule.Name}' does not have an active conjugated π-system (found {conjAtoms.Count} conjugated atom(s)).");
        }

        int n = conjAtoms.Count;
        var atomMap = new Dictionary<int, int>(); // Molecule atom index -> conjugated index (0..n-1)
        for (int i = 0; i < n; i++) atomMap[conjAtoms[i].AtomIndex] = i;

        // 2. Build Hamiltonian matrix H in units of β where H_ii = h_i and H_ij = k_ij
        double[,] H = new double[n, n];
        int totalPiElectrons = 0;
        var coreCharges = new double[n];
        var symbols = new string[n];

        for (int i = 0; i < n; i++)
        {
            var ca = conjAtoms[i];
            symbols[i] = ca.Symbol;
            H[i, i] = ca.CoulombH;
            coreCharges[i] = ca.PiElectronsContributed;
            totalPiElectrons += ca.PiElectronsContributed;
        }

        // Add resonance integrals for bonded pairs
        foreach (var bond in molecule.Bonds)
        {
            if (atomMap.TryGetValue(bond.Atom1Index, out int u) && atomMap.TryGetValue(bond.Atom2Index, out int v))
            {
                // Check if bond is conjugated (double, triple, aromatic, or single bond connecting conjugated atoms)
                double k = GetResonanceIntegralK(conjAtoms[u], conjAtoms[v], bond.Type);
                H[u, v] = k;
                H[v, u] = k;
            }
        }

        return SolveHuckel(molecule.Name, H, coreCharges, symbols, conjAtoms.Select(a => a.AtomIndex).ToList(), molecule, betaEv);
    }

    /// <summary>
    /// Computes Hückel electronic structure directly from an explicit Hamiltonian matrix in β units.
    /// </summary>
    public static HuckelResult AnalyzeMatrix(
        string name,
        double[,] H,
        int[] electronContributions,
        string[]? atomLabels = null,
        double betaEv = DefaultBetaEv)
    {
        ArgumentNullException.ThrowIfNull(H);
        ArgumentNullException.ThrowIfNull(electronContributions);

        int n = electronContributions.Length;
        if (H.GetLength(0) != n || H.GetLength(1) != n)
        {
            throw new ArgumentException("Hamiltonian matrix dimensions must match electron contributions length.");
        }

        atomLabels ??= Enumerable.Range(1, n).Select(i => $"C{i}").ToArray();
        var coreCharges = electronContributions.Select(e => (double)e).ToArray();
        var atomIndices = Enumerable.Range(0, n).ToList();

        return SolveHuckel(name, H, coreCharges, atomLabels, atomIndices, null, betaEv);
    }

    private static HuckelResult SolveHuckel(
        string moleculeName,
        double[,] H,
        double[] coreCharges,
        string[] symbols,
        List<int> atomIndices,
        Molecule? originalMolecule,
        double betaEv)
    {
        int n = coreCharges.Length;
        int totalPiElectrons = (int)Math.Round(coreCharges.Sum());

        // 3. Diagonalize symmetric Hamiltonian via exact Jacobi eigensolver
        var (eigenvalues, eigenvectors) = JacobiEigensolver.Diagonalize(H);

        // Sort orbitals in descending order of energy eigenvalue x_k (bonding with x > 0 first)
        // Since energy epsilon_k = alpha + x_k * beta (and beta < 0), larger x_k = lower physical energy (bonding).
        var orbitalIndices = Enumerable.Range(0, n).OrderByDescending(k => eigenvalues[k]).ToList();

        var sortedEigenvalues = new double[n];
        var sortedEigenvectors = new double[n, n];

        for (int rank = 0; rank < n; rank++)
        {
            int origIdx = orbitalIndices[rank];
            sortedEigenvalues[rank] = eigenvalues[origIdx];
            for (int r = 0; r < n; r++)
            {
                sortedEigenvectors[rank, r] = eigenvectors[r, origIdx];
            }
        }

        // 4. Assign electron occupancy via Aufbau principle (2 per orbital)
        var occupancy = new int[n];
        int remainingElectrons = totalPiElectrons;
        int homoIndex = -1;
        int lumoIndex = -1;

        for (int rank = 0; rank < n; rank++)
        {
            if (remainingElectrons >= 2)
            {
                occupancy[rank] = 2;
                remainingElectrons -= 2;
                homoIndex = rank;
            }
            else if (remainingElectrons == 1)
            {
                occupancy[rank] = 1;
                remainingElectrons -= 1;
                homoIndex = rank;
            }
            else
            {
                occupancy[rank] = 0;
                if (lumoIndex == -1) lumoIndex = rank;
            }
        }

        if (lumoIndex == -1 && n > 0)
        {
            lumoIndex = Math.Min(n - 1, (homoIndex >= 0 ? homoIndex + 1 : 0));
        }

        // 5. Construct Molecular Orbitals
        var orbitals = new List<MolecularOrbital>();
        double totalPiEnergyBeta = 0.0;

        for (int rank = 0; rank < n; rank++)
        {
            double x = sortedEigenvalues[rank];
            int occ = occupancy[rank];
            totalPiEnergyBeta += occ * x;

            double energyEv = -x * betaEv; // Physical energy relative to alpha in eV
            string orbType = x > 1e-4 ? "Bonding (π)" : (x < -1e-4 ? "Antibonding (π*)" : "Non-Bonding (n)");

            var coeffs = new List<double>();
            for (int r = 0; r < n; r++) coeffs.Add(sortedEigenvectors[rank, r]);

            orbitals.Add(new MolecularOrbital(
                OrbitalIndex: rank + 1,
                EnergyAlphaCoeff: 1.0,
                EnergyBetaCoeff: Math.Round(x, 4),
                EnergyEv: Math.Round(energyEv, 3),
                ElectronOccupancy: occ,
                Coefficients: coeffs,
                OrbitalType: orbType
            ));
        }

        // 6. HOMO-LUMO Gap & UV-Vis λ_max
        double homoBeta = homoIndex >= 0 ? sortedEigenvalues[homoIndex] : 0.0;
        double lumoBeta = lumoIndex >= 0 ? sortedEigenvalues[lumoIndex] : 0.0;
        double gapBeta = Math.Max(0.0, homoBeta - lumoBeta);
        double gapEv = gapBeta * betaEv;
        double uvVisMaxNm = gapEv > 0.01 ? (PlanckSpeedOfLightConstantNmEv / gapEv) : 0.0;

        // 7. Dewar Aromatic Delocalization / Resonance Energy
        // Localized reference: isolated double bonds (each has 2 electrons with E = 2α + 2β)
        int isolatedDoubleBonds = totalPiElectrons / 2;
        double localizedEnergyBeta = isolatedDoubleBonds * 2.0;
        double resonanceEnergyBeta = Math.Round(totalPiEnergyBeta - localizedEnergyBeta, 4);
        double resonanceEnergyKcal = Math.Round(resonanceEnergyBeta * DefaultBetaKcalPerMol, 2);

        // 8. Coulson π-Bond Orders
        var bondOrders = new List<PiBondOrder>();
        for (int r = 0; r < n; r++)
        {
            for (int s = r + 1; s < n; s++)
            {
                if (Math.Abs(H[r, s]) > 1e-5)
                {
                    double p_rs = 0.0;
                    for (int k = 0; k < n; k++)
                    {
                        if (occupancy[k] > 0)
                        {
                            p_rs += occupancy[k] * sortedEigenvectors[k, r] * sortedEigenvectors[k, s];
                        }
                    }

                    // Empirical Coulson-Salem bond length relation: R = 1.517 - 0.18 * p_rs (Å)
                    double bondLength = Math.Round(1.517 - 0.18 * p_rs, 3);

                    bondOrders.Add(new PiBondOrder(
                        Atom1Index: atomIndices[r],
                        Atom1Symbol: symbols[r],
                        Atom2Index: atomIndices[s],
                        Atom2Symbol: symbols[s],
                        BondOrder: Math.Round(p_rs, 4),
                        EstimatedBondLengthAngstrom: bondLength
                    ));
                }
            }
        }

        // 9. Atomic π-Electron Density & Net Charges
        var atomCharges = new List<AtomPiCharge>();
        var fukuiIndices = new List<FukuiIndex>();

        for (int r = 0; r < n; r++)
        {
            double q_r = 0.0;
            for (int k = 0; k < n; k++)
            {
                if (occupancy[k] > 0)
                {
                    q_r += occupancy[k] * sortedEigenvectors[k, r] * sortedEigenvectors[k, r];
                }
            }

            double netCharge = coreCharges[r] - q_r;

            atomCharges.Add(new AtomPiCharge(
                AtomIndex: atomIndices[r],
                Symbol: symbols[r],
                CoreCharge: coreCharges[r],
                PiElectronDensity: Math.Round(q_r, 4),
                NetCharge: Math.Round(netCharge, 4)
            ));

            // Fukui Reactivity Indices
            double fMinus = homoIndex >= 0 ? Math.Pow(sortedEigenvectors[homoIndex, r], 2) : 0.0;
            double fPlus = lumoIndex >= 0 ? Math.Pow(sortedEigenvectors[lumoIndex, r], 2) : 0.0;
            double fZero = (fMinus + fPlus) / 2.0;

            fukuiIndices.Add(new FukuiIndex(
                AtomIndex: atomIndices[r],
                Symbol: symbols[r],
                ElectrophilicAttackIndex: Math.Round(fMinus, 4),
                NucleophilicAttackIndex: Math.Round(fPlus, 4),
                RadicalAttackIndex: Math.Round(fZero, 4)
            ));
        }

        return new HuckelResult(
            MoleculeName: moleculeName,
            ConjugatedAtomCount: n,
            TotalPiElectrons: totalPiElectrons,
            Orbitals: orbitals,
            HomoIndex: homoIndex + 1,
            LumoIndex: lumoIndex + 1,
            HomoEnergyBetaCoeff: Math.Round(homoBeta, 4),
            LumoEnergyBetaCoeff: Math.Round(lumoBeta, 4),
            HomoLumoGapBetaCoeff: Math.Round(gapBeta, 4),
            HomoLumoGapEv: Math.Round(gapEv, 3),
            EstimatedUvVisMaxWavelengthNm: Math.Round(uvVisMaxNm, 1),
            TotalPiEnergyBetaCoeff: Math.Round(totalPiEnergyBeta, 4),
            LocalizedPiEnergyBetaCoeff: Math.Round(localizedEnergyBeta, 4),
            DewarResonanceEnergyBetaCoeff: resonanceEnergyBeta,
            DewarResonanceEnergyKcalPerMol: resonanceEnergyKcal,
            PiBondOrders: bondOrders,
            AtomCharges: atomCharges,
            FukuiIndices: fukuiIndices
        );
    }

    private record ConjugatedAtom(
        int AtomIndex,
        string Symbol,
        double CoulombH,
        int PiElectronsContributed
    );

    private static List<ConjugatedAtom> IdentifyConjugatedAtoms(Molecule molecule)
    {
        var list = new List<ConjugatedAtom>();
        int nAtoms = molecule.Atoms.Count;

        for (int i = 0; i < nAtoms; i++)
        {
            var atom = molecule.Atoms[i];
            string sym = atom.Element.Symbol;
            if (sym == "H") continue;

            var bonds = molecule.Bonds.Where(b => b.Connects(i)).ToList();
            bool hasMultipleBond = bonds.Any(b => b.Type is BondType.Double or BondType.Triple or BondType.Aromatic);

            if (sym == "C")
            {
                if (hasMultipleBond)
                {
                    list.Add(new ConjugatedAtom(i, "C", 0.0, 1));
                }
            }
            else if (sym == "N")
            {
                if (hasMultipleBond)
                {
                    // Pyridine-like =N- (contributes 1 electron, h_N = 0.5)
                    list.Add(new ConjugatedAtom(i, "N", 0.5, 1));
                }
                else
                {
                    // Check if adjacent to conjugated atom -> Pyrrole/amino-like (contributes 2 electrons, h_N = 1.5)
                    bool adjToConjugated = bonds.Any(b => {
                        int nbr = b.Atom1Index == i ? b.Atom2Index : b.Atom1Index;
                        return molecule.Bonds.Any(nb => nb.Connects(nbr) && nb.Type != BondType.Single);
                    });
                    if (adjToConjugated)
                    {
                        list.Add(new ConjugatedAtom(i, "N", 1.5, 2));
                    }
                }
            }
            else if (sym == "O")
            {
                if (hasMultipleBond)
                {
                    // Carbonyl =O (contributes 1 electron, h_O = 1.0)
                    list.Add(new ConjugatedAtom(i, "O", 1.0, 1));
                }
                else
                {
                    // Furan/ether/hydroxyl -O- (contributes 2 electrons, h_O = 2.0)
                    bool adjToConjugated = bonds.Any(b => {
                        int nbr = b.Atom1Index == i ? b.Atom2Index : b.Atom1Index;
                        return molecule.Bonds.Any(nb => nb.Connects(nbr) && nb.Type != BondType.Single);
                    });
                    if (adjToConjugated)
                    {
                        list.Add(new ConjugatedAtom(i, "O", 2.0, 2));
                    }
                }
            }
            else if (sym == "S")
            {
                if (hasMultipleBond)
                {
                    list.Add(new ConjugatedAtom(i, "S", 0.0, 1));
                }
                else
                {
                    list.Add(new ConjugatedAtom(i, "S", 1.0, 2));
                }
            }
            else if (sym is "F" or "Cl" or "Br")
            {
                double h = sym switch { "F" => 3.0, "Cl" => 2.0, _ => 1.5 };
                list.Add(new ConjugatedAtom(i, sym, h, 2));
            }
        }

        return list;
    }

    private static double GetResonanceIntegralK(ConjugatedAtom a1, ConjugatedAtom a2, BondType bondType)
    {
        double baseK = bondType switch
        {
            BondType.Triple => 1.2,
            _ => 1.0 // Standard simple HMO treats all conjugated C-C bonds with k = 1.0
        };

        // Heteroatom scaling factor (Streitwieser standard parameters)
        string pair = $"{a1.Symbol}-{a2.Symbol}";
        if (pair is "C-N" or "N-C")
        {
            return a1.PiElectronsContributed == 2 || a2.PiElectronsContributed == 2 ? 0.8 * baseK : 1.0 * baseK;
        }
        if (pair is "C-O" or "O-C")
        {
            return a1.PiElectronsContributed == 2 || a2.PiElectronsContributed == 2 ? 0.8 * baseK : 1.0 * baseK;
        }
        if (pair is "C-F" or "F-C") return 0.7 * baseK;
        if (pair is "C-Cl" or "Cl-C") return 0.4 * baseK;

        return baseK;
    }
}

/// <summary>
/// Exact, deterministic Jacobi symmetric matrix eigensolver.
/// Computes all eigenvalues and eigenvectors for real symmetric matrices with machine-precision quadratic convergence.
/// </summary>
public static class JacobiEigensolver
{
    private const double Epsilon = 1e-15;
    private const int MaxSweeps = 100;

    public static (double[] Eigenvalues, double[,] Eigenvectors) Diagonalize(double[,] matrix)
    {
        int n = matrix.GetLength(0);
        double[,] A = (double[,])matrix.Clone();
        double[,] V = new double[n, n];

        // Initialize V as identity matrix
        for (int i = 0; i < n; i++) V[i, i] = 1.0;

        for (int sweep = 0; sweep < MaxSweeps; sweep++)
        {
            double maxOffDiag = 0.0;

            for (int p = 0; p < n - 1; p++)
            {
                for (int q = p + 1; q < n; q++)
                {
                    double apq = A[p, q];
                    double absApq = Math.Abs(apq);
                    if (absApq > maxOffDiag) maxOffDiag = absApq;

                    if (absApq < Epsilon) continue;

                    double app = A[p, p];
                    double aqq = A[q, q];

                    double tau = (aqq - app) / (2.0 * apq);
                    double t;
                    if (tau >= 0.0)
                    {
                        t = 1.0 / (tau + Math.Sqrt(1.0 + tau * tau));
                    }
                    else
                    {
                        t = -1.0 / (-tau + Math.Sqrt(1.0 + tau * tau));
                    }

                    double c = 1.0 / Math.Sqrt(1.0 + t * t);
                    double s = t * c;
                    double h = t * apq;

                    A[p, p] -= h;
                    A[q, q] += h;
                    A[p, q] = 0.0;
                    A[q, p] = 0.0;

                    for (int r = 0; r < n; r++)
                    {
                        if (r != p && r != q)
                        {
                            double arp = A[r, p];
                            double arq = A[r, q];
                            A[r, p] = c * arp - s * arq;
                            A[p, r] = A[r, p];
                            A[r, q] = s * arp + c * arq;
                            A[q, r] = A[r, q];
                        }
                    }

                    for (int r = 0; r < n; r++)
                    {
                        double vrp = V[r, p];
                        double vrq = V[r, q];
                        V[r, p] = c * vrp - s * vrq;
                        V[r, q] = s * vrp + c * vrq;
                    }
                }
            }

            if (maxOffDiag < Epsilon) break;
        }

        var eigenvalues = new double[n];
        for (int i = 0; i < n; i++) eigenvalues[i] = A[i, i];

        return (eigenvalues, V);
    }
}
