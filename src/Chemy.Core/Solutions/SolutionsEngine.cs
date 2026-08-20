namespace Chemy.Core.Solutions;

/// <summary>
/// Encapsulates the results of aqueous pH and pOH calculations.
/// </summary>
/// <param name="Ph">Potential of Hydrogen (pH = -log10[H+]).</param>
/// <param name="Poh">Potential of Hydroxide (pOH = 14 - pH).</param>
/// <param name="HConcentrationMolar">Hydronium ion concentration [H+] in Molar (M).</param>
/// <param name="OhConcentrationMolar">Hydroxide ion concentration [OH-] in Molar (M).</param>
/// <param name="IsAcidic">True if pH &lt; 7.0.</param>
/// <param name="IsBasic">True if pH &gt; 7.0.</param>
/// <param name="IsNeutral">True if pH ≈ 7.0.</param>
public record PhResult(
    double Ph,
    double Poh,
    double HConcentrationMolar,
    double OhConcentrationMolar,
    bool IsAcidic,
    bool IsBasic,
    bool IsNeutral
)
{
    /// <summary>Formats the pH result as a string.</summary>
    public override string ToString() => $"pH = {Ph:F2}, pOH = {Poh:F2} ({(IsAcidic ? "Acidic" : IsBasic ? "Basic" : "Neutral")})";
}

/// <summary>
/// Encapsulates Henderson-Hasselbalch buffer solution calculations.
/// </summary>
/// <param name="Ph">Calculated equilibrium buffer pH.</param>
/// <param name="Pka">Acid dissociation constant pKa.</param>
/// <param name="AcidConcentrationMolar">Weak acid concentration [HA] in Molar (M).</param>
/// <param name="ConjugateBaseConcentrationMolar">Conjugate base concentration [A-] in Molar (M).</param>
public record BufferResult(
    double Ph,
    double Pka,
    double AcidConcentrationMolar,
    double ConjugateBaseConcentrationMolar
)
{
    /// <summary>Formats the buffer result as a string.</summary>
    public override string ToString() => $"Buffer pH = {Ph:F2} (pK_a = {Pka:F2})";
}

/// <summary>
/// Industrial-Grade Solutions Chemistry &amp; Acid-Base Equilibria Engine.
/// Solves aqueous pH, pOH, strong/weak acid dissociations, and Henderson-Hasselbalch buffer equations.
/// </summary>
public static class SolutionsEngine
{
    /// <summary>
    /// Calculates pH, pOH, [H+], and [OH-] for a monoprotic strong acid including water autodissociation (Kw = 1.0e-14).
    /// Exact quadratic equilibrium formula: [H+] = (C + sqrt(C^2 + 4*Kw)) / 2.
    /// </summary>
    /// <param name="concentrationMolar">Analytical acid concentration in Molar (M).</param>
    /// <returns>PhResult record.</returns>
    public static PhResult CalculateStrongAcidPh(double concentrationMolar)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(concentrationMolar);

        const double Kw = 1.0e-14;
        double hConc = (concentrationMolar + Math.Sqrt((concentrationMolar * concentrationMolar) + (4.0 * Kw))) / 2.0;
        double ph = -Math.Log10(hConc);
        double poh = 14.0 - ph;
        double ohConc = Kw / hConc;

        return new PhResult(ph, poh, hConc, ohConc, ph < 7.0, ph > 7.0, Math.Abs(ph - 7.0) < 0.01);
    }

    /// <summary>
    /// Calculates pH for a weak acid via exact cubic polynomial equilibrium solver:
    /// [H+]³ + Ka*[H+]² - (Kw + Ka*C)*[H+] - Ka*Kw = 0.
    /// Solves the full equilibrium considering both acid dissociation and water autoionization across all dilutions.
    /// </summary>
    /// <param name="concentrationMolar">Analytical acid concentration in Molar (M).</param>
    /// <param name="ka">Acid dissociation constant Ka.</param>
    /// <returns>PhResult record.</returns>
    public static PhResult CalculateWeakAcidPh(double concentrationMolar, double ka)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(concentrationMolar);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ka);

        const double Kw = 1.0e-14;

        // Solve cubic: f(x) = x³ + Ka*x² - (Kw + Ka*C)*x - Ka*Kw = 0
        // Initial estimate from Ostwald dilution or water autoionization
        double x = Math.Max(1e-7, (-ka + Math.Sqrt((ka * ka) + (4.0 * ka * concentrationMolar))) / 2.0);

        // Halley's high-order root-finding method for cubic equilibrium
        for (int iter = 0; iter < 20; iter++)
        {
            double f = (x * x * x) + (ka * x * x) - ((Kw + ka * concentrationMolar) * x) - (ka * Kw);
            double df = (3.0 * x * x) + (2.0 * ka * x) - (Kw + ka * concentrationMolar);
            double d2f = (6.0 * x) + (2.0 * ka);

            double step = (2.0 * f * df) / ((2.0 * df * df) - (f * d2f));
            x -= step;

            if (Math.Abs(step) < 1e-15 * x || Math.Abs(f) < 1e-25)
            {
                break;
            }
        }

        double hConc = Math.Max(1e-14, x);
        double ph = -Math.Log10(hConc);
        double poh = 14.0 - ph;
        double ohConc = Kw / hConc;

        return new PhResult(ph, poh, hConc, ohConc, ph < 7.0, ph > 7.0, Math.Abs(ph - 7.0) < 0.01);
    }

    /// <summary>
    /// Solves the Henderson-Hasselbalch buffer equation: pH = pKa + log10([A-] / [HA]).
    /// </summary>
    /// <param name="pka">Acid dissociation constant pKa.</param>
    /// <param name="acidConcentrationMolar">Weak acid concentration [HA] (M).</param>
    /// <param name="conjugateBaseConcentrationMolar">Conjugate base concentration [A-] (M).</param>
    /// <returns>BufferResult record.</returns>
    public static BufferResult CalculateBufferPh(double pka, double acidConcentrationMolar, double conjugateBaseConcentrationMolar)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(acidConcentrationMolar);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(conjugateBaseConcentrationMolar);

        double ph = pka + Math.Log10(conjugateBaseConcentrationMolar / acidConcentrationMolar);

        return new BufferResult(ph, pka, acidConcentrationMolar, conjugateBaseConcentrationMolar);
    }
}
