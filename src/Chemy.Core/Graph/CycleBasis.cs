namespace Chemy.Core.Graph;

using Chemy.Core.Scientific;
using Chemy.Core.Structure;

/// <summary>
/// Encapsulates the Smallest Set of Smallest Rings (SSSR) cycle basis for a molecular graph.
/// </summary>
/// <param name="Rings">List of minimal cycle vertex paths.</param>
/// <param name="FrerejacqueNumber">Cyclomatic number: M = E - V + C (fundamental cycle basis dimension).</param>
/// <param name="MethodInfo">Scientific method provenance and metadata.</param>
public sealed record SssrResult(
    IReadOnlyList<IReadOnlyList<int>> Rings,
    int FrerejacqueNumber,
    ScientificMethodInfo MethodInfo
);

/// <summary>
/// Smallest Set of Smallest Rings (SSSR) Cycle Basis Engine.
/// Implements Horton's polynomial-time minimum cycle basis algorithm on molecular graphs.
/// Reference: Horton, J. D. (1987). A polynomial-time algorithm to find the shortest cycle basis of a graph.
/// SIAM Journal on Computing, 16(2), 358-366.
/// </summary>
public static class CycleBasis
{
    private static readonly ScientificMethodInfo SssrMethodInfo = new(
        "Horton Minimum Cycle Basis (SSSR)",
        "1987.1",
        EvidenceLevel.ExactEquation,
        "Connected or disconnected molecular graphs with arbitrary cycle topologies.",
        ["Deterministic polynomial-time cycle basis extraction."]
    );

    /// <summary>
    /// Computes the authentic Smallest Set of Smallest Rings (SSSR) for a molecule.
    /// </summary>
    public static SssrResult ComputeSssr(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        int vCount = molecule.Atoms.Count;
        int eCount = molecule.Bonds.Count;
        if (vCount < 3 || eCount < 3)
        {
            return new SssrResult(Array.Empty<IReadOnlyList<int>>(), 0, SssrMethodInfo);
        }

        // 1. Calculate number of connected components
        int components = CountComponents(molecule);
        int cyclomaticNumber = eCount - vCount + components; // Frèrejacque number

        if (cyclomaticNumber <= 0)
        {
            return new SssrResult(Array.Empty<IReadOnlyList<int>>(), 0, SssrMethodInfo);
        }

        // 2. Build adjacency list
        var adj = new List<int>[vCount];
        for (int i = 0; i < vCount; i++) adj[i] = new List<int>();

        foreach (var b in molecule.Bonds)
        {
            adj[b.Atom1Index].Add(b.Atom2Index);
            adj[b.Atom2Index].Add(b.Atom1Index);
        }

        // 3. Horton's candidate cycle generation via BFS shortest-path trees from all vertices
        var candidateCycles = new List<List<int>>();

        for (int root = 0; root < vCount; root++)
        {
            var (dist, parent) = BfsTree(root, adj, vCount);

            // For every edge (u, v) not in the shortest-path tree rooted at 'root'
            foreach (var b in molecule.Bonds)
            {
                int u = b.Atom1Index;
                int v = b.Atom2Index;

                if (parent[u] != v && parent[v] != u && dist[u] < int.MaxValue && dist[v] < int.MaxValue)
                {
                    // Form cycle: Path(root -> u) + edge(u, v) + Path(v -> root)
                    var cycle = ReconstructCycle(root, u, v, parent);
                    if (cycle.Count >= 3)
                    {
                        candidateCycles.Add(cycle);
                    }
                }
            }
        }

        // 4. Canonicalize candidate cycles and sort by length (smallest first)
        var uniqueSorted = candidateCycles
            .Select(CanonicalizeCycle)
            .GroupBy(c => string.Join(",", c))
            .Select(g => g.First())
            .OrderBy(c => c.Count)
            .ToList();

        // 5. Greedily select linearly independent cycles over GF(2) edge incidence vectors
        var basis = new List<IReadOnlyList<int>>();
        var edgeVectors = new List<bool[]>();

        // Map bonds to fixed indices 0..eCount-1
        var bondIndexMap = new Dictionary<(int, int), int>();
        for (int bIdx = 0; bIdx < molecule.Bonds.Count; bIdx++)
        {
            var b = molecule.Bonds[bIdx];
            int u = Math.Min(b.Atom1Index, b.Atom2Index);
            int v = Math.Max(b.Atom1Index, b.Atom2Index);
            bondIndexMap[(u, v)] = bIdx;
        }

        foreach (var cycle in uniqueSorted)
        {
            var vec = new bool[eCount];
            for (int k = 0; k < cycle.Count; k++)
            {
                int u = Math.Min(cycle[k], cycle[(k + 1) % cycle.Count]);
                int v = Math.Max(cycle[k], cycle[(k + 1) % cycle.Count]);
                if (bondIndexMap.TryGetValue((u, v), out int edgeIdx))
                {
                    vec[edgeIdx] = true;
                }
            }

            if (IsLinearlyIndependent(vec, edgeVectors, eCount))
            {
                basis.Add(cycle);
                edgeVectors.Add(vec);

                if (basis.Count == cyclomaticNumber)
                {
                    break;
                }
            }
        }

        return new SssrResult(basis, cyclomaticNumber, SssrMethodInfo);
    }

    private static (int[] Dist, int[] Parent) BfsTree(int root, List<int>[] adj, int n)
    {
        var dist = new int[n];
        var parent = new int[n];
        Array.Fill(dist, int.MaxValue);
        Array.Fill(parent, -1);

        dist[root] = 0;
        var queue = new Queue<int>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            foreach (int v in adj[u])
            {
                if (dist[v] == int.MaxValue)
                {
                    dist[v] = dist[u] + 1;
                    parent[v] = u;
                    queue.Enqueue(v);
                }
            }
        }

        return (dist, parent);
    }

    private static List<int> ReconstructCycle(int root, int u, int v, int[] parent)
    {
        var pathU = new List<int>();
        int curr = u;
        while (curr != -1)
        {
            pathU.Add(curr);
            if (curr == root) break;
            curr = parent[curr];
        }

        var pathV = new List<int>();
        curr = v;
        while (curr != -1)
        {
            pathV.Add(curr);
            if (curr == root) break;
            curr = parent[curr];
        }

        // Find Lowest Common Ancestor (LCA)
        int lca = root;
        int iU = pathU.Count - 1;
        int iV = pathV.Count - 1;
        while (iU >= 0 && iV >= 0 && pathU[iU] == pathV[iV])
        {
            lca = pathU[iU];
            iU--;
            iV--;
        }

        var cycle = new List<int>();
        for (int k = 0; k <= iU + 1; k++) cycle.Add(pathU[k]);
        for (int k = iV; k >= 0; k--) cycle.Add(pathV[k]);

        return cycle;
    }

    private static List<int> CanonicalizeCycle(List<int> cycle)
    {
        int n = cycle.Count;
        int minElem = cycle.Min();
        int minIdx = cycle.IndexOf(minElem);

        // Forward
        var forward = new List<int>(n);
        for (int i = 0; i < n; i++) forward.Add(cycle[(minIdx + i) % n]);

        // Backward
        var backward = new List<int>(n);
        for (int i = 0; i < n; i++) backward.Add(cycle[(minIdx - i + n) % n]);

        for (int i = 0; i < n; i++)
        {
            if (forward[i] < backward[i]) return forward;
            if (backward[i] < forward[i]) return backward;
        }

        return forward;
    }

    private static bool IsLinearlyIndependent(bool[] newVec, List<bool[]> basis, int dim)
    {
        // Gaussian elimination over GF(2)
        var mat = basis.Select(b => (bool[])b.Clone()).ToList();
        var target = (bool[])newVec.Clone();

        foreach (var row in mat)
        {
            // Find leading 1 in row
            int lead = -1;
            for (int j = 0; j < dim; j++)
            {
                if (row[j]) { lead = j; break; }
            }

            if (lead >= 0 && target[lead])
            {
                // XOR row into target
                for (int j = 0; j < dim; j++)
                {
                    target[j] ^= row[j];
                }
            }
        }

        return target.Any(b => b); // Non-zero vector means linearly independent
    }

    private static int CountComponents(Molecule molecule)
    {
        int n = molecule.Atoms.Count;
        var visited = new bool[n];
        int count = 0;

        for (int i = 0; i < n; i++)
        {
            if (!visited[i])
            {
                count++;
                var q = new Queue<int>();
                q.Enqueue(i);
                visited[i] = true;

                while (q.Count > 0)
                {
                    int u = q.Dequeue();
                    foreach (var b in molecule.Bonds)
                    {
                        if (b.Connects(u))
                        {
                            int v = b.Atom1Index == u ? b.Atom2Index : b.Atom1Index;
                            if (!visited[v])
                            {
                                visited[v] = true;
                                q.Enqueue(v);
                            }
                        }
                    }
                }
            }
        }

        return count;
    }
}
