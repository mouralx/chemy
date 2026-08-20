namespace Chemy.Core.Kinetics;

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
);

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
        var points = new List<TimeStepPoint>();
        double dt = totalTime / steps;

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

            points.Add(new TimeStepPoint(
                Math.Round(t, 2),
                Math.Round(Math.Max(0, a), 4),
                Math.Round(Math.Max(0, b), 4),
                Math.Round(Math.Max(0, c), 4)
            ));
        }

        return new ReactionNetworkSimulationResult("Consecutive Cascade (A -> B -> C)", k1, k2, totalTime, points);
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
        double dt = totalTime / steps;

        double[] time = new double[steps + 1];
        double[][] trajectories = new double[steps + 1][];

        double[] c = (double[])initialConcentrations.Clone();
        time[0] = 0.0;
        trajectories[0] = (double[])c.Clone();

        for (int step = 0; step < steps; step++)
        {
            // k1 = f(c)
            double[] k1 = rateLaw(c);

            // k2 = f(c + 0.5*dt*k1)
            double[] c_k1 = new double[n];
            for (int i = 0; i < n; i++) c_k1[i] = Math.Max(0.0, c[i] + 0.5 * dt * k1[i]);
            double[] k2 = rateLaw(c_k1);

            // k3 = f(c + 0.5*dt*k2)
            double[] c_k2 = new double[n];
            for (int i = 0; i < n; i++) c_k2[i] = Math.Max(0.0, c[i] + 0.5 * dt * k2[i]);
            double[] k3 = rateLaw(c_k2);

            // k4 = f(c + dt*k3)
            double[] c_k3 = new double[n];
            for (int i = 0; i < n; i++) c_k3[i] = Math.Max(0.0, c[i] + dt * k3[i]);
            double[] k4 = rateLaw(c_k3);

            // c_{new} = c + (dt/6) * (k1 + 2*k2 + 2*k3 + k4)
            for (int i = 0; i < n; i++)
            {
                c[i] = Math.Max(0.0, c[i] + (dt / 6.0) * (k1[i] + (2.0 * k2[i]) + (2.0 * k3[i]) + k4[i]));
            }

            time[step + 1] = Math.Round((step + 1) * dt, 4);
            trajectories[step + 1] = (double[])c.Clone();
        }

        return (time, trajectories);
    }
}
