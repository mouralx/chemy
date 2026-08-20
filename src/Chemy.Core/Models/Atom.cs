namespace Chemy.Core;

/// <summary>
/// Represents an immutable atomic particle composed of an elemental nucleus (protons and neutrons)
/// and surrounding electron shell. Supports isotopic mass numbers and ionic charge calculations.
/// </summary>
public record Atom
{
    /// <summary>The fundamental chemical element defining the atomic identity and proton count.</summary>
    public Element Element { get; init; }

    /// <summary>Count of neutrons located in the atomic nucleus.</summary>
    public int Neutrons { get; init; }

    /// <summary>Count of electrons occupying electron shells.</summary>
    public int Electrons { get; init; }

    /// <summary>Proton count derived directly from the element's atomic number (Z).</summary>
    public int Protons => Element.AtomicNumber;

    /// <summary>Isotopic mass number (A = Protons + Neutrons).</summary>
    public int MassNumber => Protons + Neutrons;

    /// <summary>Net electrostatic charge (Protons - Electrons).</summary>
    public int NetCharge => Protons - Electrons;

    /// <summary>True if the atom carries a non-zero net electrical charge.</summary>
    public bool IsIon => NetCharge != 0;

    /// <summary>True if net charge is positive (electron deficiency).</summary>
    public bool IsCation => NetCharge > 0;

    /// <summary>True if net charge is negative (electron excess).</summary>
    public bool IsAnion => NetCharge < 0;

    /// <summary>
    /// Constructs a neutral atom with electron count equal to proton count (Z).
    /// </summary>
    /// <param name="element">Elemental identity.</param>
    /// <param name="neutrons">Neutron count in the nucleus.</param>
    public Atom(Element element, int neutrons)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(neutrons);

        Element = element;
        Neutrons = neutrons;
        Electrons = element.AtomicNumber;
    }

    /// <summary>
    /// Constructs an atom or ion with an explicit electron count.
    /// </summary>
    /// <param name="element">Elemental identity.</param>
    /// <param name="neutrons">Neutron count.</param>
    /// <param name="electrons">Electron count.</param>
    public Atom(Element element, int neutrons, int electrons)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(neutrons);
        ArgumentOutOfRangeException.ThrowIfNegative(electrons);

        Element = element;
        Neutrons = neutrons;
        Electrons = electrons;
    }

    /// <summary>
    /// Produces a new atom instance by adding or removing electrons.
    /// </summary>
    /// <param name="deltaElectrons">Electrons removed (positive) or added (negative).</param>
    /// <returns>New ionized Atom instance.</returns>
    public Atom Ionize(int deltaElectrons) => this with { Electrons = Math.Max(0, Electrons - deltaElectrons) };

    /// <summary>
    /// Produces a new isotopic atom instance with modified neutron count.
    /// </summary>
    /// <param name="newNeutronCount">Target neutron count.</param>
    /// <returns>New isotopic Atom instance.</returns>
    public Atom WithNeutrons(int newNeutronCount) => this with { Neutrons = newNeutronCount };

    /// <summary>
    /// Formats the atom in standard IUPAC isotopic notation (e.g. ^12C, ^1H+, ^16O2-).
    /// </summary>
    public override string ToString()
    {
        string chargeStr = NetCharge switch
        {
            0 => string.Empty,
            1 => "+",
            -1 => "-",
            > 0 => $"+{NetCharge}",
            _ => $"{NetCharge}"
        };

        return $"^{MassNumber}{Element.Symbol}{chargeStr}";
    }
}