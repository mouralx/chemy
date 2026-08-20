namespace Chemy.Core.Structure;

public enum FunctionalGroup
{
    Alcohol,
    CarboxylicAcid,
    Ester,
    Aldehyde,
    Ketone,
    Ether,
    Amine,
    Amide,
    Nitrile,
    Nitro,
    Alkene,
    Alkyne,
    Aromatic
}

public static class FunctionalGroupDetector
{
    public static IReadOnlySet<FunctionalGroup> Detect(Molecule molecule)
    {
        ArgumentNullException.ThrowIfNull(molecule);

        var detected = new HashSet<FunctionalGroup>();

        for (int i = 0; i < molecule.Atoms.Count; i++)
        {
            var atom = molecule.Atoms[i];

            if (atom.Element.Symbol == "C")
            {
                var bondedToC = GetNeighbors(molecule, i);

                var doubleO = bondedToC.FirstOrDefault(n => n.Atom.Element.Symbol == "O" && n.Bond.Type == BondType.Double);
                if (doubleO != null)
                {
                    var singleO = bondedToC.FirstOrDefault(n => n.Atom.Element.Symbol == "O" && n.Bond.Type == BondType.Single);
                    var singleN = bondedToC.FirstOrDefault(n => n.Atom.Element.Symbol == "N");

                    if (singleO != null)
                    {
                        var oNeighbors = GetNeighbors(molecule, singleO.Index);
                        bool hasHOnO = oNeighbors.Any(n => n.Atom.Element.Symbol == "H");
                        bool hasCOnO = oNeighbors.Any(n => n.Index != i && n.Atom.Element.Symbol == "C");

                        if (hasHOnO) detected.Add(FunctionalGroup.CarboxylicAcid);
                        else if (hasCOnO) detected.Add(FunctionalGroup.Ester);
                    }
                    else if (singleN != null)
                    {
                        detected.Add(FunctionalGroup.Amide);
                    }
                    else
                    {
                        bool hasHOnC = bondedToC.Any(n => n.Atom.Element.Symbol == "H");
                        var carbonNeighbors = bondedToC.Where(n => n.Atom.Element.Symbol == "C").ToList();

                        if (hasHOnC) detected.Add(FunctionalGroup.Aldehyde);
                        else if (carbonNeighbors.Count >= 2) detected.Add(FunctionalGroup.Ketone);
                    }
                }

                // Check for Nitrile (C≡N)
                var tripleN = bondedToC.FirstOrDefault(n => n.Atom.Element.Symbol == "N" && n.Bond.Type == BondType.Triple);
                if (tripleN != null)
                {
                    detected.Add(FunctionalGroup.Nitrile);
                }
            }
            else if (atom.Element.Symbol == "O")
            {
                var neighbors = GetNeighbors(molecule, i);
                bool hasH = neighbors.Any(n => n.Atom.Element.Symbol == "H");
                var carbons = neighbors.Where(n => n.Atom.Element.Symbol == "C").ToList();

                if (hasH && carbons.Count == 1)
                {
                    int cIdx = carbons[0].Index;
                    var cNeighbors = GetNeighbors(molecule, cIdx);
                    bool isCarbonyl = cNeighbors.Any(n => n.Atom.Element.Symbol == "O" && n.Bond.Type == BondType.Double);

                    if (!isCarbonyl) detected.Add(FunctionalGroup.Alcohol);
                }
                else if (carbons.Count == 2)
                {
                    bool isEsterOrAcid = carbons.Any(c => GetNeighbors(molecule, c.Index).Any(n => n.Atom.Element.Symbol == "O" && n.Bond.Type == BondType.Double));
                    if (!isEsterOrAcid) detected.Add(FunctionalGroup.Ether);
                }
            }
            else if (atom.Element.Symbol == "N")
            {
                var neighbors = GetNeighbors(molecule, i);
                bool isAmide = neighbors.Any(n => n.Atom.Element.Symbol == "C" && GetNeighbors(molecule, n.Index).Any(cn => cn.Atom.Element.Symbol == "O" && cn.Bond.Type == BondType.Double));
                bool isNitrile = neighbors.Any(n => n.Bond.Type == BondType.Triple);
                int oxygenCount = neighbors.Count(n => n.Atom.Element.Symbol == "O");

                if (oxygenCount >= 2)
                {
                    detected.Add(FunctionalGroup.Nitro);
                }
                else if (isNitrile)
                {
                    detected.Add(FunctionalGroup.Nitrile);
                }
                else if (isAmide)
                {
                    detected.Add(FunctionalGroup.Amide);
                }
                else if (neighbors.Any(n => n.Atom.Element.Symbol == "C"))
                {
                    detected.Add(FunctionalGroup.Amine);
                }
            }
        }

        foreach (var bond in molecule.Bonds)
        {
            if (bond.Type == BondType.Double)
            {
                var a1 = molecule.Atoms[bond.Atom1Index];
                var a2 = molecule.Atoms[bond.Atom2Index];
                if (a1.Element.Symbol == "C" && a2.Element.Symbol == "C")
                {
                    detected.Add(FunctionalGroup.Alkene);
                }
            }
            else if (bond.Type == BondType.Triple)
            {
                var a1 = molecule.Atoms[bond.Atom1Index];
                var a2 = molecule.Atoms[bond.Atom2Index];
                if (a1.Element.Symbol == "C" && a2.Element.Symbol == "C")
                {
                    detected.Add(FunctionalGroup.Alkyne);
                }
            }
            else if (bond.Type == BondType.Aromatic)
            {
                detected.Add(FunctionalGroup.Aromatic);
            }
        }

        return detected;
    }

    private sealed record Neighbor(int Index, Atom Atom, Bond Bond);


    private static List<Neighbor> GetNeighbors(Molecule molecule, int atomIndex)
    {
        var list = new List<Neighbor>();
        foreach (var bond in molecule.Bonds)
        {
            if (bond.Atom1Index == atomIndex)
            {
                list.Add(new Neighbor(bond.Atom2Index, molecule.Atoms[bond.Atom2Index], bond));
            }
            else if (bond.Atom2Index == atomIndex)
            {
                list.Add(new Neighbor(bond.Atom1Index, molecule.Atoms[bond.Atom1Index], bond));
            }
        }

        return list;
    }
}
