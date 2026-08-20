namespace Chemy.Core.Reactions;

/// <summary>
/// Exact Rational Matrix Solver for Chemical Equation Balancing.
/// Solves the nullspace vector equation M * x = 0 over the rational field Q,
/// using Gaussian elimination with partial pivoting and integer LCM scaling.
/// </summary>
internal static class MatrixSolver
{
    /// <summary>
    /// Computes the minimal strictly positive integer nullspace solution vector for matrix M.
    /// </summary>
    /// <param name="matrix">Stoichiometric element conservation matrix.</param>
    /// <returns>Minimal integer coefficient vector, or null if unbalanceable.</returns>
    public static int[]? SolveNullspaceIntegerVector(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        // Convert integer matrix to exact rational matrix to prevent floating-point drift
        Rational[,] rMatrix = new Rational[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                rMatrix[r, c] = new Rational(matrix[r, c]);
            }
        }

        int pivotRow = 0;
        int[] pivotCols = new int[cols];
        Array.Fill(pivotCols, -1);

        // Gaussian elimination with partial pivoting to compute Reduced Row Echelon Form (RREF)
        for (int c = 0; c < cols && pivotRow < rows; c++)
        {
            int sel = -1;
            for (int r = pivotRow; r < rows; r++)
            {
                if (rMatrix[r, c].Num != 0)
                {
                    sel = r;
                    break;
                }
            }

            if (sel == -1) continue;

            // Swap pivot row
            for (int j = 0; j < cols; j++)
            {
                (rMatrix[pivotRow, j], rMatrix[sel, j]) = (rMatrix[sel, j], rMatrix[pivotRow, j]);
            }

            // Normalize pivot row
            Rational pivotVal = rMatrix[pivotRow, c];
            for (int j = 0; j < cols; j++)
            {
                rMatrix[pivotRow, j] = rMatrix[pivotRow, j] / pivotVal;
            }

            // Eliminate other rows in current column
            for (int r = 0; r < rows; r++)
            {
                if (r != pivotRow && rMatrix[r, c].Num != 0)
                {
                    Rational factor = rMatrix[r, c];
                    for (int j = 0; j < cols; j++)
                    {
                        rMatrix[r, j] = rMatrix[r, j] - factor * rMatrix[pivotRow, j];
                    }
                }
            }

            pivotCols[c] = pivotRow;
            pivotRow++;
        }

        // Identify free variable column
        int freeCol = -1;
        for (int c = cols - 1; c >= 0; c--)
        {
            if (pivotCols[c] == -1)
            {
                freeCol = c;
                break;
            }
        }

        if (freeCol == -1) return null;

        // Assign free variable unit rational value and back-substitute
        Rational[] solution = new Rational[cols];
        solution[freeCol] = new Rational(1);

        for (int c = 0; c < cols; c++)
        {
            if (pivotCols[c] != -1)
            {
                int r = pivotCols[c];
                solution[c] = -rMatrix[r, freeCol];
            }
        }

        // Compute Least Common Multiple (LCM) of denominators to clear fractions
        long lcm = 1;
        foreach (var rat in solution)
        {
            if (rat.Num != 0)
            {
                lcm = Lcm(lcm, rat.Den);
            }
        }

        // Scale by LCM into integer solution
        int[] intVector = new int[cols];
        bool allPositive = true;
        for (int c = 0; c < cols; c++)
        {
            Rational scaled = solution[c] * new Rational(lcm);
            int val = (int)(scaled.Num / scaled.Den);
            if (val <= 0) allPositive = false;
            intVector[c] = val;
        }

        if (!allPositive)
        {
            for (int c = 0; c < cols; c++)
            {
                intVector[c] = -intVector[c];
                if (intVector[c] <= 0) return null;
            }
        }

        // Reduce by Greatest Common Divisor (GCD) to minimal primitive integers
        long commonGcd = intVector[0];
        for (int c = 1; c < cols; c++)
        {
            commonGcd = Gcd(commonGcd, intVector[c]);
        }

        if (commonGcd > 1)
        {
            for (int c = 0; c < cols; c++)
            {
                intVector[c] /= (int)commonGcd;
            }
        }

        return intVector;
    }

    /// <summary>Calculates Greatest Common Divisor (Euclidean algorithm).</summary>
    private static long Gcd(long a, long b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            long temp = b;
            b = a % b;
            a = temp;
        }
        return a == 0 ? 1 : a;
    }

    /// <summary>Calculates Least Common Multiple.</summary>
    private static long Lcm(long a, long b) => (a / Gcd(a, b)) * b;
}

/// <summary>
/// Immutable, exact rational number struct with automatic GCD reduction.
/// </summary>
internal readonly struct Rational
{
    public long Num { get; }
    public long Den { get; }

    public Rational(long num, long den = 1)
    {
        if (den == 0) throw new DivideByZeroException("Rational denominator cannot be zero.");

        if (den < 0)
        {
            num = -num;
            den = -den;
        }

        long g = Gcd(num, den);
        Num = num / g;
        Den = den / g;
    }

    public static Rational operator +(Rational a, Rational b) => new(a.Num * b.Den + b.Num * a.Den, a.Den * b.Den);
    public static Rational operator -(Rational a, Rational b) => new(a.Num * b.Den - b.Num * a.Den, a.Den * b.Den);
    public static Rational operator -(Rational a) => new(-a.Num, a.Den);
    public static Rational operator *(Rational a, Rational b) => new(a.Num * b.Num, a.Den * b.Den);
    public static Rational operator /(Rational a, Rational b) => new(a.Num * b.Den, a.Den * b.Num);

    private static long Gcd(long a, long b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            long temp = b;
            b = a % b;
            a = temp;
        }
        return a == 0 ? 1 : a;
    }
}
