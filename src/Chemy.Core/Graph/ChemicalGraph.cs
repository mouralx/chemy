using System.Collections.Immutable;

namespace Chemy.Core.Graph;

/// <summary>
/// Represents a node in the chemical molecular graph with elemental identity, formal charge, and connectivity.
/// </summary>
public record GraphNode(int Id, Element Element, int FormalCharge = 0, int ImplicitHydrogens = 0)
{
    /// <summary>Calculated atomic mass including implicit hydrogens.</summary>
    public double TotalMass => Element.StandardAtomicMass + (ImplicitHydrogens * 1.008);
}

/// <summary>
/// Represents an edge in the chemical molecular graph with formal bond order and aromaticity flag.
/// </summary>
public record GraphEdge(int SourceId, int TargetId, BondType BondType, bool IsAromatic = false)
{
    /// <summary>True if edge is incident to the given node ID.</summary>
    public bool Connects(int nodeId) => SourceId == nodeId || TargetId == nodeId;

    /// <summary>Returns the adjacent neighbor node ID connected via this edge.</summary>
    public int Other(int nodeId) => SourceId == nodeId ? TargetId : SourceId;
}

/// <summary>
/// Industrial-grade, immutable topological chemical graph representation.
/// Provides adjacency indexing, cycle detection (SSSR / Hansch rings), and degree calculations.
/// </summary>
public class ChemicalGraph
{
    private readonly ImmutableList<GraphNode> _nodes;
    private readonly ImmutableList<GraphEdge> _edges;
    private readonly ImmutableDictionary<int, ImmutableList<GraphEdge>> _adjacency;

    /// <summary>Read-only collection of graph nodes.</summary>
    public IReadOnlyList<GraphNode> Nodes => _nodes;

    /// <summary>Read-only collection of graph edges.</summary>
    public IReadOnlyList<GraphEdge> Edges => _edges;

    /// <summary>Total count of vertices (atoms) in the graph.</summary>
    public int NodeCount => _nodes.Count;

    /// <summary>Total count of edges (bonds) in the graph.</summary>
    public int EdgeCount => _edges.Count;

    /// <summary>
    /// Constructs an immutable ChemicalGraph instance from nodes and edges.
    /// </summary>
    public ChemicalGraph(IEnumerable<GraphNode> nodes, IEnumerable<GraphEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        _nodes = nodes.ToImmutableList();
        _edges = edges.ToImmutableList();

        var adjBuilder = new Dictionary<int, List<GraphEdge>>();
        foreach (var node in _nodes)
        {
            adjBuilder[node.Id] = new List<GraphEdge>();
        }

        foreach (var edge in _edges)
        {
            if (adjBuilder.TryGetValue(edge.SourceId, out var srcList))
                srcList.Add(edge);

            if (adjBuilder.TryGetValue(edge.TargetId, out var tgtList))
                tgtList.Add(edge);
        }

        _adjacency = adjBuilder.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableList());
    }

    /// <summary>
    /// Converts a standard Chemy Molecule instance into an indexed ChemicalGraph.
    /// </summary>
    public static ChemicalGraph FromMolecule(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        if (!molecule.HasBondedTopology)
        {
            throw new InvalidOperationException(
                $"Molecule '{molecule.Name}' has no bonded topology. Chemical graph operations require a bonded molecular graph (e.g. from SMILES or Molfile/SDF), not an empirical formula without connectivity.");
        }

        var nodes = molecule.Atoms.Select((a, idx) => new GraphNode(idx, a.Element, a.NetCharge, 0)).ToList();
        var edges = molecule.Bonds.Select(b => new GraphEdge(b.Atom1Index, b.Atom2Index, b.Type, b.Type == BondType.Aromatic)).ToList();

        return new ChemicalGraph(nodes, edges);
    }

    /// <summary>
    /// Gets all edges incident to the specified node ID.
    /// </summary>
    public IReadOnlyList<GraphEdge> GetIncidentEdges(int nodeId) =>
        _adjacency.TryGetValue(nodeId, out var edges) ? edges : ImmutableList<GraphEdge>.Empty;

    /// <summary>
    /// Gets all neighbor node IDs adjacent to the specified node ID.
    /// </summary>
    public IReadOnlyList<int> GetNeighbors(int nodeId) =>
        GetIncidentEdges(nodeId).Select(e => e.Other(nodeId)).ToList();

    /// <summary>
    /// Returns the coordination degree (valence connectivity) of a node.
    /// </summary>
    public int GetDegree(int nodeId) => GetIncidentEdges(nodeId).Count;

    /// <summary>
    /// Detects all fundamental cycles (rings) in the molecular graph using Depth-First Search (DFS).
    /// </summary>
    public IReadOnlyList<IReadOnlyList<int>> FindRings()
    {
        var rings = new List<IReadOnlyList<int>>();
        var visited = new HashSet<int>();
        var parent = new Dictionary<int, int>();
        var stack = new List<int>();

        foreach (var node in _nodes)
        {
            if (!visited.Contains(node.Id))
            {
                DfsCycle(node.Id, -1, visited, parent, stack, rings);
            }
        }

        return rings;
    }

    private void DfsCycle(int current, int p, HashSet<int> visited, Dictionary<int, int> parent, List<int> stack, List<IReadOnlyList<int>> rings)
    {
        visited.Add(current);
        parent[current] = p;
        stack.Add(current);

        foreach (var neighbor in GetNeighbors(current))
        {
            if (neighbor == p) continue;

            if (stack.Contains(neighbor))
            {
                // Found back-edge, extract cycle
                int startIndex = stack.IndexOf(neighbor);
                var cycle = stack.Skip(startIndex).ToList();
                if (cycle.Count >= 3)
                {
                    // Avoid duplicate permutations of the same cycle
                    var normalized = NormalizeCycle(cycle);
                    if (!rings.Any(r => AreCyclesEqual(r, normalized)))
                    {
                        rings.Add(normalized);
                    }
                }
            }
            else if (!visited.Contains(neighbor))
            {
                DfsCycle(neighbor, current, visited, parent, stack, rings);
            }
        }

        stack.RemoveAt(stack.Count - 1);
    }

    private static List<int> NormalizeCycle(List<int> cycle)
    {
        int minVal = cycle.Min();
        int minIdx = cycle.IndexOf(minVal);
        var rotated = cycle.Skip(minIdx).Concat(cycle.Take(minIdx)).ToList();
        return rotated;
    }

    private static bool AreCyclesEqual(IReadOnlyList<int> c1, IReadOnlyList<int> c2)
    {
        if (c1.Count != c2.Count) return false;
        return c1.SequenceEqual(c2) || c1.SequenceEqual(c2.Reverse());
    }
}
