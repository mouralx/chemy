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

        var atoms = graph.Nodes.Select(n =>
        {
            int defaultNeutrons = Math.Max(0, (int)Math.Round(n.Element.StandardAtomicMass) - n.Element.AtomicNumber);
            var a = new Atom(n.Element, defaultNeutrons);
            if (n.FormalCharge != 0) a = a.Ionize(n.FormalCharge);
            return a;
        }).ToList();

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

        IReadOnlyDictionary<int, int> firstMatch;
        int carbonId, carbonylO, hydroxylO;
        var hydroxylH = new HashSet<int>();

        if (matches.Count > 0)
        {
            firstMatch = matches[0];
            carbonId = firstMatch[0];
            carbonylO = firstMatch[1];
            hydroxylO = firstMatch[2];
            if (firstMatch.TryGetValue(3, out int hIdx))
            {
                hydroxylH.Add(hIdx);
            }
        }
        else
        {
            // Fallback for carboxylate without explicit H: match carboxyl where O is not bonded to another Carbon (not an ester)
            var groupMatches = SubgraphMatcher.FindMatches(graph, SubgraphMatcher.CarboxylGroupQuery);
            var validAcidMatch = groupMatches.FirstOrDefault(m =>
            {
                int sO = m[2];
                // Check that single-bonded oxygen is NOT bonded to another carbon (which would make it an ester)
                return !graph.GetIncidentEdges(sO).Any(e => e.Other(sO) != m[0] && graph.Nodes[e.Other(sO)].Element.Symbol == "C");
            });

            if (validAcidMatch == null) return source;

            firstMatch = validAcidMatch;
            carbonId = firstMatch[0];
            carbonylO = firstMatch[1];
            hydroxylO = firstMatch[2];
        }

        // Also identify any explicit hydrogen attached to the hydroxyl oxygen
        foreach (var edge in graph.GetIncidentEdges(hydroxylO))
        {
            int other = edge.Other(hydroxylO);
            if (other < graph.Nodes.Count && graph.Nodes[other].Element.Symbol == "H")
            {
                hydroxylH.Add(other);
            }
        }

        // Find attachment point (node connecting to the carboxyl carbon that is not the oxygen atoms)
        var attachmentEdge = graph.GetIncidentEdges(carbonId)
            .FirstOrDefault(e => e.Other(carbonId) != carbonylO && e.Other(carbonId) != hydroxylO);

        int attachmentNodeId = attachmentEdge?.Other(carbonId) ?? -1;

        // Build new node list omitting the old carboxyl C, O, O (and acid proton)
        var nodesToOmit = new HashSet<int>(hydroxylH) { carbonId, carbonylO, hydroxylO };
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

        // Add 1H-tetrazole 5-membered ring: C(0) - N(1) - N(2) - N(3) - N(4) - C(0) with N4-H proton
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
        int tetrazoleH = newNodes.Count;
        newNodes.Add(new GraphNode(tetrazoleH, Elements.Hydrogen));

        // Connect tetrazole ring bonds
        newEdges.Add(new GraphEdge(tetrazoleC, tetrazoleN1, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN1, tetrazoleN2, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN2, tetrazoleN3, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN3, tetrazoleN4, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN4, tetrazoleC, BondType.Aromatic, true));
        newEdges.Add(new GraphEdge(tetrazoleN4, tetrazoleH, BondType.Single, false));

        // Connect attachment node to the tetrazole ring carbon
        if (attachmentNodeId >= 0 && oldToNewIndex.TryGetValue(attachmentNodeId, out int remappedAttachment))
        {
            newEdges.Add(new GraphEdge(remappedAttachment, tetrazoleC, BondType.Single));
        }

        var newGraph = new ChemicalGraph(newNodes, newEdges);
        return ToMolecule(newGraph, $"{source.Name} (Tetrazole Bioisostere)");
    }

    /// <summary>
    /// Substitutes a hydrogen atom with fluorine (para-fluorination / metabolic shield) preserving carbon valence.
    /// </summary>
    public static Molecule AppendFluorineShield(Molecule source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var atoms = source.Atoms.ToList();
        var bonds = source.Bonds.ToList();

        // 1. Locate an aromatic C-H bond if available
        int hydrogenToReplace = -1;
        for (int i = 0; i < atoms.Count; i++)
        {
            if (atoms[i].Element.Symbol == "H")
            {
                var connectedBonds = bonds.Where(b => b.Connects(i)).ToList();
                if (connectedBonds.Count == 1)
                {
                    int partnerIdx = connectedBonds[0].Atom1Index == i ? connectedBonds[0].Atom2Index : connectedBonds[0].Atom1Index;
                    if (atoms[partnerIdx].Element.Symbol == "C")
                    {
                        bool isAromatic = bonds.Any(b => b.Connects(partnerIdx) && b.Type == BondType.Aromatic);
                        if (isAromatic)
                        {
                            hydrogenToReplace = i;
                            break;
                        }
                        if (hydrogenToReplace < 0)
                        {
                            hydrogenToReplace = i;
                        }
                    }
                }
            }
        }

        if (hydrogenToReplace >= 0)
        {
            // Replace existing Hydrogen with Fluorine (preserves valence and octet rule)
            atoms[hydrogenToReplace] = new Atom(Elements.Fluorine, 10);
            return new Molecule($"{source.Name} (Fluorinated Lead)", atoms, bonds);
        }

        // Fallback if no explicit hydrogen is present (e.g. implicit H SMILES without H atoms)
        int targetIndex = atoms.FindIndex(a => a.Element.Symbol == "C");
        if (targetIndex < 0) targetIndex = 0;

        int fluorineIndex = atoms.Count;
        atoms.Add(new Atom(Elements.Fluorine, 10));
        bonds.Add(new Bond(targetIndex, fluorineIndex, BondType.Single));

        return new Molecule($"{source.Name} (Fluorinated Lead)", atoms, bonds);
    }
}
