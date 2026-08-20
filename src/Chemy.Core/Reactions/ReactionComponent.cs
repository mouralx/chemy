namespace Chemy.Core;

/// <summary>
/// Represents a stoichiometric component (molecule and integer coefficient) in a chemical reaction equation.
/// </summary>
/// <param name="Molecule">Constituent molecule participating in the reaction.</param>
/// <param name="Coefficient">Stoichiometric integer coefficient (default: 1).</param>
public record ReactionComponent(Molecule Molecule, int Coefficient = 1)
{
    /// <summary>Formats the reaction component as 'n[Molecule]' or '[Molecule]'.</summary>
    public override string ToString() => Coefficient == 1 ? Molecule.Name : $"{Coefficient}{Molecule.Name}";
}
