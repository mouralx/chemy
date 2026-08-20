namespace Chemy.Core;

/// <summary>
/// Lightweight, immutable stack-allocated representation of an IUPAC chemical element.
/// </summary>
/// <param name="AtomicNumber">Atomic number (Z), representing the number of nuclear protons.</param>
/// <param name="Symbol">Standard IUPAC 1-to-2 letter chemical symbol (e.g. H, Fe, Og).</param>
/// <param name="Name">Full English element name (e.g. Hydrogen, Iron, Oganesson).</param>
/// <param name="StandardAtomicMass">Standard atomic weight in unified atomic mass units (u or g/mol).</param>
public readonly record struct Element(int AtomicNumber, string Symbol, string Name, double StandardAtomicMass);