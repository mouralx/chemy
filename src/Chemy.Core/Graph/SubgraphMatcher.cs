namespace Chemy.Core.Graph;

/// <summary>
/// Subgraph query template representing a chemical motif or reactive pharmacophore.
/// </summary>
public record SubgraphQuery(
    string Name,
    IReadOnlyList<string> ElementSymbols,
    IReadOnlyList<(int Node1, int Node2, BondType? BondType)> RequiredBonds
);

/// <summary>
/// Subgraph Isomorphism &amp; Pattern Matching Engine.
/// Identifies target functional groups and pharmacophore substructures directly on chemical graphs.
/// </summary>
public static class SubgraphMatcher
{
    /// <summary>Predefined query pattern for Carboxylic Acid (-C(=O)O-H).</summary>
    public static readonly SubgraphQuery CarboxylicAcidQuery = new(
        "CarboxylicAcid",
        ["C", "O", "O", "H"],
        [(0, 1, BondType.Double), (0, 2, BondType.Single), (2, 3, BondType.Single)]
    );

    /// <summary>Query pattern for Carboxyl group (-C(=O)O) without explicit hydrogen requirement.</summary>
    public static readonly SubgraphQuery CarboxylGroupQuery = new(
        "CarboxylGroup",
        ["C", "O", "O"],
        [(0, 1, BondType.Double), (0, 2, BondType.Single)]
    );

    /// <summary>Predefined query pattern for Ester (-C(=O)O-C).</summary>
    public static readonly SubgraphQuery EsterQuery = new(
        "Ester",
        ["C", "O", "O", "C"],
        [(0, 1, BondType.Double), (0, 2, BondType.Single), (2, 3, BondType.Single)]
    );

    /// <summary>Predefined query pattern for Amide (-C(=O)N-).</summary>
    public static readonly SubgraphQuery AmideQuery = new(
        "Amide",
        ["C", "O", "N"],
        [(0, 1, BondType.Double), (0, 2, BondType.Single)]
    );

    /// <summary>Predefined query pattern for Ketone / Carbonyl (-C-C(=O)-C-).</summary>
    public static readonly SubgraphQuery KetoneQuery = new(
        "Ketone",
        ["C", "O"],
        [(0, 1, BondType.Double)]
    );

    /// <summary>
    /// Searches for all subgraph occurrences of a target query within a target ChemicalGraph.
    /// </summary>
    /// <param name="graph">Target molecular graph.</param>
    /// <param name="query">Subgraph query motif.</param>
    /// <returns>List of matching node ID mappings (query node index -> target graph node ID).</returns>
    public static IReadOnlyList<IReadOnlyDictionary<int, int>> FindMatches(ChemicalGraph graph, SubgraphQuery query)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(query);

        var matches = new List<IReadOnlyDictionary<int, int>>();
        int qCount = query.ElementSymbols.Count;

        // Candidate node lists per query node
        var candidateLists = new List<List<int>>();
        for (int q = 0; q < qCount; q++)
        {
            string reqSym = query.ElementSymbols[q];
            var cands = graph.Nodes
                .Where(n => string.Equals(n.Element.Symbol, reqSym, StringComparison.OrdinalIgnoreCase))
                .Select(n => n.Id)
                .ToList();

            if (cands.Count == 0) return matches;
            candidateLists.Add(cands);
        }

        // Backtracking depth-first search for subgraph mapping
        var currentMapping = new Dictionary<int, int>();
        SearchMappings(0, qCount, candidateLists, query, graph, currentMapping, matches);

        return matches;
    }

    private static void SearchMappings(
        int qIdx,
        int qCount,
        List<List<int>> candidateLists,
        SubgraphQuery query,
        ChemicalGraph graph,
        Dictionary<int, int> currentMapping,
        List<IReadOnlyDictionary<int, int>> matches)
    {
        if (qIdx == qCount)
        {
            // Verify all required query bonds are satisfied in target graph
            if (VerifyBonds(currentMapping, query, graph))
            {
                matches.Add(new Dictionary<int, int>(currentMapping));
            }
            return;
        }

        foreach (var targetNodeId in candidateLists[qIdx])
        {
            if (currentMapping.ContainsValue(targetNodeId)) continue; // Must be injective

            currentMapping[qIdx] = targetNodeId;

            // Early pruning check for bonds between already mapped nodes
            if (IsPartiallyValid(currentMapping, query, graph, qIdx))
            {
                SearchMappings(qIdx + 1, qCount, candidateLists, query, graph, currentMapping, matches);
            }

            currentMapping.Remove(qIdx);
        }
    }

    private static bool IsPartiallyValid(Dictionary<int, int> mapping, SubgraphQuery query, ChemicalGraph graph, int maxQIdx)
    {
        foreach (var (n1, n2, expectedBond) in query.RequiredBonds)
        {
            if (n1 <= maxQIdx && n2 <= maxQIdx)
            {
                int t1 = mapping[n1];
                int t2 = mapping[n2];

                var edge = graph.GetIncidentEdges(t1).FirstOrDefault(e => e.Other(t1) == t2);
                if (edge == null) return false;

                if (expectedBond.HasValue && edge.BondType != expectedBond.Value)
                    return false;
            }
        }

        return true;
    }

    private static bool VerifyBonds(Dictionary<int, int> mapping, SubgraphQuery query, ChemicalGraph graph)
    {
        foreach (var (n1, n2, expectedBond) in query.RequiredBonds)
        {
            int t1 = mapping[n1];
            int t2 = mapping[n2];

            var edge = graph.GetIncidentEdges(t1).FirstOrDefault(e => e.Other(t1) == t2);
            if (edge == null) return false;

            if (expectedBond.HasValue && edge.BondType != expectedBond.Value)
                return false;
        }

        return true;
    }
}
