namespace Chemy.Core.Graph;

/// <summary>
/// Industrial-Grade Topological Chemical Graph Rewriting Engine.
/// Performs precise bond cleavage, atom insertion, ring substitution, and valence-consistent molecule reconstruction.
/// </summary>
public static class GraphRewriter
{
    /// <summary>
    /// Converts a ChemicalGraph instance back into a validated Chemy Molecule.
    /// </summary>
    public static Molecule ToMolecule(ChemicalGraph graph, string name = "Derivative")
    {
        ArgumentNullException.ThrowIfNull(graph);

        var atoms = graph.Nodes.Select(n => new Atom(n.Element, 0)).ToList();
        var bonds = graph.Edges.Select(e => new Bond(e.SourceId, e.TargetId, e.BondType)).ToList();

        return new Molecule(name, atoms, bonds);
    }

    /// <summary>
    /// Replaces a matched carboxylic acid (-C(=O)O) motif with a 1H-tetrazole 5-membered aromatic ring bioisostere.
    /// </summary>
    public static Molecule ReplaceCarboxylWithTetrazole(Molecule source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var graph = ChemicalGraph.FromMolecule(source);
        var matches = SubgraphMatcher.FindMatches(graph, SubgraphMatcher.CarboxylicAcidQuery);

        if (matches.Count == 0) return source;

        var firstMatch = matches[0];
        int carbonId = firstMatch[0];
        int carbonylO = firstMatch[1];
        int hydroxylO = firstMatch[2];

        // Find attachment point (node connecting to the carboxyl carbon that is not the oxygen atoms)
        var attachmentEdge = graph.GetIncidentEdges(carbonId)
            .FirstOrDefault(e => e.Other(carbonId) != carbonylO && e.Other(carbonId) != hydroxylO);

        int attachmentNodeId = attachmentEdge?.Other(carbonId) ?? -1;

        // Build new node list omitting the old carboxyl C, O, O
        var nodesToOmit = new HashSet<int> { carbonId, carbonylO, hydroxylO };
        var newNodes = new List<GraphNode>();
        var oldToNewIndex = new Dictionary<int, int>();

        foreach (var node in graph.Nodes)
        {
            if (!nodesToOmit.Contains(node.Id))
            {
                oldToNewIndex[node.Id] = newNodes.Count;
                newNodes.Add(new GraphNode(newNodes.Count, node.Element, node.FormalCharge, node.ImplicitHydrogens));
            }
        }

        // Build existing edges remapped
        var newEdges = new List<GraphEdge>();
        foreach (var edge in graph.Edges)
        {
            if (!nodesToOmit.Contains(edge.SourceId) && !nodesToOmit.Contains(edge.TargetId))
            {
                newEdges.Add(new GraphEdge(oldToNewIndex[edge.SourceId], oldToNewIndex[edge.TargetId], edge.BondType, edge.IsAromatic));
            }
        }

        // Add 1H-tetrazole 5-membered ring: C(0) - N(1) - N(2) - N(3) - N(4) - C(0)
        int tetrazoleC = newNodes.Count;
        newNodes.Add(new GraphNode(tetrazoleC, Elements.Carbon));
        int tetrazoleN1 = newNodes.Count;
        newNodes.Add(new GraphNode(tetrazoleN1, Elements.Nitrogen));
        int tetrazoleN2 = newNodes.Count;
        newNodes.Add(new GraphNode(tetrazoleN2, Elements.Nitrogen));
        int tetrazoleN3 = newNodes.Count;
        newNodes.Add(new GraphNode(tetrazoleN3, Elements.Nitrogen));
        int tetrazoleN4 = newNodes.Count;
        newNodes.Add(new GraphNode(tetrazoleN4, Elements.Nitrogen));

        // Connect tetrazole ring bonds
        newEdges.Add(new GraphEdge(tetrazoleC, tetrazoleN1, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN1, tetrazoleN2, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN2, tetrazoleN3, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN3, tetrazoleN4, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN4, tetrazoleC, BondType.Aromatic, true));

        // Connect attachment node to the tetrazole ring carbon
        if (attachmentNodeId >= 0 && oldToNewIndex.TryGetValue(attachmentNodeId, out int remappedAttachment))
        {
            newEdges.Add(new GraphEdge(remappedAttachment, tetrazoleC, BondType.Single));
        }

        var newGraph = new ChemicalGraph(newNodes, newEdges);
        return ToMolecule(newGraph, $"{source.Name} (Tetrazole Bioisostere)");
    }

    /// <summary>
    /// Attaches a fluorine atom (para-fluorination / metabolic shield) to an aromatic ring or scaffold node.
    /// </summary>
    public static Molecule AppendFluorineShield(Molecule source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var atoms = source.Atoms.ToList();
        var bonds = source.Bonds.ToList();

        int targetIndex = atoms.FindIndex(a => a.Element.Symbol == "C");
        if (targetIndex < 0) targetIndex = 0;

        int fluorineIndex = atoms.Count;
        atoms.Add(new Atom(Elements.Fluorine, 10));
        bonds.Add(new Bond(targetIndex, fluorineIndex, BondType.Single));

        return new Molecule($"{source.Name} (Fluorinated Lead)", atoms, bonds);
    }
}
