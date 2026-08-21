namespace Chemy.Core;

/// <summary>
/// Specifies the chemical bond classification and formal bond order.
/// </summary>
public enum BondType
{
    /// <summary>Single covalent sigma (σ) bond (Bond order = 1).</summary>
    Single = 1,

    /// <summary>Double covalent bond composed of 1 σ and 1 π bond (Bond order = 2).</summary>
    Double = 2,

    /// <summary>Triple covalent bond composed of 1 σ and 2 π bonds (Bond order = 3).</summary>
    Triple = 3,

    /// <summary>Delocalized aromatic conjugated bond (Bond order ≈ 1.5).</summary>
    Aromatic = 4,

    /// <summary>Electrostatic ionic interaction.</summary>
    Ionic = 5,

    /// <summary>Non-covalent hydrogen bonding interaction.</summary>
    Hydrogen = 6
}

/// <summary>
/// Lightweight, zero-allocation stack-allocated struct representing a chemical bond between two atom indices.
/// </summary>
/// <param name="Atom1Index">0-based index of the first bonded atom in the parent molecule.</param>
/// <param name="Atom2Index">0-based index of the second bonded atom in the parent molecule.</param>
/// <param name="Type">Chemical bond classification and order.</param>
public readonly record struct Bond(int Atom1Index, int Atom2Index, BondType Type = BondType.Single)
{
    /// <summary>
    /// Checks whether this bond connects to a specified atom index.
    /// </summary>
    /// <param name="index">0-based atom index to test.</param>
    /// <returns>True if either vertex of the bond matches the index.</returns>
    public bool Connects(int index) => Atom1Index == index || Atom2Index == index;

    /// <summary>
    /// Checks whether this bond connects two specified atom indices.
    /// </summary>
    public bool Connects(int u, int v) => (Atom1Index == u && Atom2Index == v) || (Atom1Index == v && Atom2Index == u);
}
