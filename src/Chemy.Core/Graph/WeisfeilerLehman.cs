namespace Chemy.Core.Graph;

using Chemy.Core.Scientific;
using Chemy.Core.Structure;

/// <summary>
/// Result of topological graph automorphism partitioning via the 1D Weisfeiler-Lehman algorithm.
/// </summary>
/// <param name="SymmetryClasses">Map of atom index to its unique canonical symmetry partition ID.</param>
/// <param name="EquivalenceGroups">List of atom index groups that are topologically indistinguishable.</param>
/// <param name="MethodInfo">Scientific provenance and method metadata.</param>
public sealed record SymmetryPartitionResult(
    IReadOnlyDictionary<int, long> SymmetryClasses,
    IReadOnlyList<IReadOnlyList<int>> EquivalenceGroups,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// 1D Weisfeiler-Lehman (WL) Graph Color Refinement Algorithm for Molecular Automorphism Perception.
/// Accurately partitions atoms into topological equivalence classes for NMR peak integration,
/// stereochemical invariance, and canonical molecular hashing.
/// </summary>
public static class WeisfeilerLehman
{
    private static readonly ScientificMethodInfo WlMethodInfo = new(
        "1D Weisfeiler-Lehman Graph Automorphism Partitioning",
        "1968.1",
        EvidenceLevel.ExactEquation,
        "General molecular graphs with arbitrary element and bond order topologies.",
        ["Iterative color refinement up to graph diameter or stable partition fixed-point."]
    );

    /// <summary>
    /// Computes the topological symmetry classes and equivalence groups of a molecule.
    /// </summary>
    public static SymmetryPartitionResult Partition(Molecule molecule, int maxIterations = 20)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        int n = molecule.Atoms.Count;
        if (n == 0)
        {
            return new SymmetryPartitionResult(new Dictionary<int, long>(), Array.Empty<IReadOnlyList<int>>(), WlMethodInfo);
        }

        // Step 1: Initial atomic colors (Atomic Number + Valence + Formal Charge)
        long[] colors = new long[n];
        for (int i = 0; i < n; i++)
        {
            var a = molecule.Atoms[i];
            int degree = molecule.Bonds.Count(b => b.Connects(i));
            colors[i] = HashInitial(a.Element.AtomicNumber, degree, a.NetCharge);
        }

        int previousClassCount = CountDistinct(colors);

        // Step 2: Iterative neighborhood multiset aggregation
        for (int iter = 0; iter < maxIterations; iter++)
        {
            long[] nextColors = new long[n];

            for (int i = 0; i < n; i++)
            {
                var neighborHashes = new List<long>();

                foreach (var b in molecule.Bonds)
                {
                    if (b.Atom1Index == i)
                    {
                        neighborHashes.Add(HashNeighbor(colors[b.Atom2Index], (int)b.Type));
                    }
                    else if (b.Atom2Index == i)
                    {
                        neighborHashes.Add(HashNeighbor(colors[b.Atom1Index], (int)b.Type));
                    }
                }

                neighborHashes.Sort(); // Multiset canonical sorting

                // Hash combination: Current color + Sorted neighbor multiset
                long combined = colors[i] * 397;
                foreach (var nh in neighborHashes)
                {
                    combined = (combined ^ nh) * 16777619;
                }

                nextColors[i] = combined;
            }

            // Normalize colors to compact ranks
            colors = CompressColors(nextColors);
            int currentClassCount = CountDistinct(colors);

            // Partition stabilized (fixed point reached)
            if (currentClassCount == previousClassCount && iter >= 3)
            {
                break;
            }
            previousClassCount = currentClassCount;
        }

        // Group atoms by their canonical symmetry class
        var classDict = new Dictionary<int, long>(n);
        var groupsMap = new Dictionary<long, List<int>>();

        for (int i = 0; i < n; i++)
        {
            classDict[i] = colors[i];
            if (!groupsMap.TryGetValue(colors[i], out var list))
            {
                list = new List<int>();
                groupsMap[colors[i]] = list;
            }
            list.Add(i);
        }

        var groups = groupsMap.Values.Select(v => (IReadOnlyList<int>)v).ToList();

        return new SymmetryPartitionResult(classDict, groups, WlMethodInfo);
    }

    private static long HashInitial(int atomicNumber, int degree, int formalCharge)
    {
        unchecked
        {
            long hash = 17;
            hash = hash * 31 + atomicNumber;
            hash = hash * 31 + degree;
            hash = hash * 31 + formalCharge;
            return hash;
        }
    }

    private static long HashNeighbor(long neighborColor, int bondType)
    {
        unchecked
        {
            return (neighborColor ^ (bondType * 31)) * 1000003;
        }
    }

    private static long[] CompressColors(long[] rawColors)
    {
        var distinctSorted = rawColors.Distinct().OrderBy(x => x).ToList();
        var mapping = new Dictionary<long, long>(distinctSorted.Count);
        for (int i = 0; i < distinctSorted.Count; i++)
        {
            mapping[distinctSorted[i]] = i + 1;
        }

        var compressed = new long[rawColors.Length];
        for (int i = 0; i < rawColors.Length; i++)
        {
            compressed[i] = mapping[rawColors[i]];
        }
        return compressed;
    }

    private static int CountDistinct(long[] array)
    {
        var set = new HashSet<long>(array);
        return set.Count;
    }
}
