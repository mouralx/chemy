namespace Chemy.Core.Kinetics;

using Chemy.Core.Scientific;

/// <summary>
/// Represents instantaneous concentrations of species in a multi-step reaction network at time t.
/// </summary>
/// <param name="TimeSeconds">Time in seconds from reaction initiation.</param>
/// <param name="ConcentrationA">Concentration of initial reactant A (M).</param>
/// <param name="ConcentrationB">Concentration of intermediate species B (M).</param>
/// <param name="ConcentrationC">Concentration of final product species C (M).</param>
public record TimeStepPoint(double TimeSeconds, double ConcentrationA, double ConcentrationB, double ConcentrationC);

/// <summary>
/// Encapsulates the results of a numerical differential equation integration run for reaction kinetics.
/// </summary>
/// <param name="CascadeType">Type of reaction cascade mechanism (e.g. Consecutive A -> B -> C).</param>
/// <param name="RateConstantK1">Forward rate constant for step 1 (A -> B).</param>
/// <param name="RateConstantK2">Forward rate constant for step 2 (B -> C).</param>
/// <param name="DurationSeconds">Total simulation duration in seconds.</param>
/// <param name="Points">Discrete time-series concentration trajectory points.</param>
public record ReactionNetworkSimulationResult(
    string CascadeType,
    double RateConstantK1,
    double RateConstantK2,
    double DurationSeconds,
    IReadOnlyList<TimeStepPoint> Points
)
{
    public ScientificMethodInfo MethodInfo { get; init; } = new(
        "Classical fourth-order Runge-Kutta integration",
        "2026.2",
        EvidenceLevel.NumericalApproximation,
        "Deterministic well-mixed concentration ODEs with constant rate coefficients and a user-specified fixed time step.",
        ["No stiffness detection; reduce the time step or use a stiff solver when the reported residual is unacceptable."]);

    public ScientificNumericalDiagnostics Diagnostics { get; init; } = new(
        false,
        double.NaN,
        double.NaN,
        double.NaN,
        "mol/L",
        "Diagnostics were not evaluated.");
}

/// <summary>
/// Multi-Step Reaction Network Kinetics Engine.
/// Solves coupled non-linear ordinary differential equations (ODEs) describing chemical kinetics
/// cascades using the 4th-Order Runge-Kutta (RK4) numerical integration algorithm.
/// </summary>
public static class ReactionNetworkEngine
{
    /// <summary>
    /// Numerically integrates a consecutive reaction cascade (A -> B -> C) via 4th-order Runge-Kutta.
    /// Rate laws: dA/dt = -k1*A, dB/dt = k1*A - k2*B, dC/dt = k2*B.
    /// </summary>
    /// <param name="initialConcA">Initial concentration of reactant A in Molar (M).</param>
    /// <param name="k1">First-order rate constant for A -> B (s⁻¹).</param>
    /// <param name="k2">First-order rate constant for B -> C (s⁻¹).</param>
    /// <param name="totalTime">Total simulation time in seconds.</param>
    /// <param name="steps">Number of discrete integration time steps.</param>
    /// <returns>Trajectory of concentrations over time.</returns>
    public static ReactionNetworkSimulationResult SimulateConsecutiveCascade(
        double initialConcA = 1.0,
        double k1 = 0.5,
        double k2 = 0.2,
        double totalTime = 10.0,
        int steps = 50)
    {
        if (!double.IsFinite(initialConcA) || initialConcA < 0.0) throw new ArgumentOutOfRangeException(nameof(initialConcA));
        if (!double.IsFinite(k1) || k1 < 0.0) throw new ArgumentOutOfRangeException(nameof(k1));
        if (!double.IsFinite(k2) || k2 < 0.0) throw new ArgumentOutOfRangeException(nameof(k2));
        if (!double.IsFinite(totalTime) || totalTime <= 0.0) throw new ArgumentOutOfRangeException(nameof(totalTime));
        if (steps <= 0) throw new ArgumentOutOfRangeException(nameof(steps));

        var points = new List<TimeStepPoint>();
        double dt = totalTime / steps;
        double maximumResidual = 0.0;
        double maximumConservationError = 0.0;

        double a = initialConcA;
        double b = 0.0;
        double c = 0.0;
        double t = 0.0;

        points.Add(new TimeStepPoint(t, a, b, c));

        // RK4 Butcher Tableau integration steps
        for (int i = 0; i < steps; i++)
        {
            // k1 slope evaluation at beginning of interval
            double da1 = -k1 * a;
            double db1 = k1 * a - k2 * b;
            double dc1 = k2 * b;

            // k2 slope evaluation at midpoint with Euler step
            double da2 = -k1 * (a + 0.5 * dt * da1);
            double db2 = k1 * (a + 0.5 * dt * da1) - k2 * (b + 0.5 * dt * db1);
            double dc2 = k2 * (b + 0.5 * dt * db1);

            // k3 slope evaluation at midpoint with improved Euler step
            double da3 = -k1 * (a + 0.5 * dt * da2);
            double db3 = k1 * (a + 0.5 * dt * da2) - k2 * (b + 0.5 * dt * db2);
            double dc3 = k2 * (b + 0.5 * dt * db2);

            // k4 slope evaluation at end of interval
            double da4 = -k1 * (a + dt * da3);
            double db4 = k1 * (a + dt * da3) - k2 * (b + dt * db3);
            double dc4 = k2 * (b + dt * db3);

            // Weighted RK4 Simpson rule combination
            a += (dt / 6.0) * (da1 + 2 * da2 + 2 * da3 + da4);
            b += (dt / 6.0) * (db1 + 2 * db2 + 2 * db3 + db4);
            c += (dt / 6.0) * (dc1 + 2 * dc2 + 2 * dc3 + dc4);
            t += dt;

            double expectedA = initialConcA * Math.Exp(-k1 * t);
            double expectedB = Math.Abs(k1 - k2) < 1e-14
                ? initialConcA * k1 * t * Math.Exp(-k1 * t)
                : initialConcA * k1 * (Math.Exp(-k1 * t) - Math.Exp(-k2 * t)) / (k2 - k1);
            double expectedC = initialConcA - expectedA - expectedB;
            maximumResidual = Math.Max(maximumResidual, Math.Abs(a - expectedA));
            maximumResidual = Math.Max(maximumResidual, Math.Abs(b - expectedB));
            maximumResidual = Math.Max(maximumResidual, Math.Abs(c - expectedC));
            maximumConservationError = Math.Max(maximumConservationError, Math.Abs((a + b + c) - initialConcA));

            points.Add(new TimeStepPoint(
                t,
                a,
                b,
                c
            ));
        }

        double convergenceThreshold = Math.Max(1e-10, initialConcA * 1e-4);
        return new ReactionNetworkSimulationResult("Consecutive Cascade (A -> B -> C)", k1, k2, totalTime, points)
        {
            Diagnostics = new ScientificNumericalDiagnostics(
                maximumResidual <= convergenceThreshold,
                dt,
                maximumResidual,
                maximumConservationError,
                "mol/L",
                "Residual is measured against the analytical consecutive first-order solution; convergence threshold is 1e-4 of initial concentration.")
        };
    }

    /// <summary>
    /// Numerically integrates an arbitrary N-species reaction network ODE system: dC/dt = f(C, t) via RK4.
    /// </summary>
    /// <param name="initialConcentrations">Array of initial concentrations for all N chemical species.</param>
    /// <param name="rateLaw">Delegate calculating the time derivatives dC/dt given current state C.</param>
    /// <param name="totalTime">Total simulation duration in seconds.</param>
    /// <param name="steps">Number of discrete time steps.</param>
    /// <returns>Matrix of trajectories [steps + 1, N species].</returns>
    public static (double[] Time, double[][] Trajectories) SimulateGeneralNetwork(
        double[] initialConcentrations,
        Func<double[], double[]> rateLaw,
        double totalTime = 10.0,
        int steps = 100)
    {
        ArgumentNullException.ThrowIfNull(initialConcentrations);
        ArgumentNullException.ThrowIfNull(rateLaw);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalTime);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);

        int n = initialConcentrations.Length;
        if (n == 0) throw new ArgumentException("At least one species concentration is required.", nameof(initialConcentrations));
        if (initialConcentrations.Any(value => !double.IsFinite(value) || value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(initialConcentrations), "Initial concentrations must be finite and non-negative.");
        }
        double dt = totalTime / steps;

        double[] time = new double[steps + 1];
        double[][] trajectories = new double[steps + 1][];

        double[] c = (double[])initialConcentrations.Clone();
        time[0] = 0.0;
        trajectories[0] = (double[])c.Clone();

        for (int step = 0; step < steps; step++)
        {
            // k1 = f(c)
            double[] k1 = ValidateDerivative(rateLaw((double[])c.Clone()), n);

            // k2 = f(c + 0.5*dt*k1)
            double[] c_k1 = new double[n];
            for (int i = 0; i < n; i++) c_k1[i] = c[i] + 0.5 * dt * k1[i];
            ValidateStageState(c_k1, step, "midpoint-1");
            double[] k2 = ValidateDerivative(rateLaw((double[])c_k1.Clone()), n);

            // k3 = f(c + 0.5*dt*k2)
            double[] c_k2 = new double[n];
            for (int i = 0; i < n; i++) c_k2[i] = c[i] + 0.5 * dt * k2[i];
            ValidateStageState(c_k2, step, "midpoint-2");
            double[] k3 = ValidateDerivative(rateLaw((double[])c_k2.Clone()), n);

            // k4 = f(c + dt*k3)
            double[] c_k3 = new double[n];
            for (int i = 0; i < n; i++) c_k3[i] = c[i] + dt * k3[i];
            ValidateStageState(c_k3, step, "endpoint");
            double[] k4 = ValidateDerivative(rateLaw((double[])c_k3.Clone()), n);

            // c_{new} = c + (dt/6) * (k1 + 2*k2 + 2*k3 + k4)
            for (int i = 0; i < n; i++)
            {
                double next = c[i] + (dt / 6.0) * (k1[i] + (2.0 * k2[i]) + (2.0 * k3[i]) + k4[i]);
                if (!double.IsFinite(next) || next < -1e-12)
                {
                    throw new InvalidOperationException(
                        $"RK4 produced an invalid concentration for species {i} at step {step + 1}; reduce the time step or use a stiff solver.");
                }
                c[i] = Math.Max(0.0, next);
            }

            time[step + 1] = (step + 1) * dt;
            trajectories[step + 1] = (double[])c.Clone();
        }

        return (time, trajectories);
    }

    private static double[] ValidateDerivative(double[] derivative, int expectedLength)
    {
        if (derivative is null || derivative.Length != expectedLength)
        {
            throw new InvalidOperationException($"Rate law must return exactly {expectedLength} finite derivatives.");
        }
        if (derivative.Any(value => !double.IsFinite(value)))
        {
            throw new InvalidOperationException("Rate law returned a non-finite derivative.");
        }
        return (double[])derivative.Clone();
    }

    private static void ValidateStageState(double[] state, int step, string stage)
    {
        for (int species = 0; species < state.Length; species++)
        {
            if (!double.IsFinite(state[species]) || state[species] < -1e-12)
            {
                throw new InvalidOperationException(
                    $"RK4 produced an invalid concentration for species {species} at {stage} of step {step + 1}; reduce the time step or use a stiff solver.");
            }
            if (state[species] < 0.0) state[species] = 0.0;
        }
    }
}
