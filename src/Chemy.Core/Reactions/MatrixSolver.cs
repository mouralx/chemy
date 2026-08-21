namespace Chemy.Core.Reactions;

using System.Numerics;

/// <summary>
/// Exact Rational Matrix Solver for Chemical Stoichiometry and Nullspace Basis Decomposition.
/// Computes the complete nullspace basis of M * x = 0 over the rational field Q,
/// transforming underdetermined multi-reaction systems into fundamental independent reaction pathways.
/// </summary>
public static class MatrixSolver
{
    /// <summary>
    /// Computes the minimal strictly positive integer nullspace solution vector for matrix M (if nullity = 1).
    /// </summary>
    public static int[]? SolveNullspaceIntegerVector(int[,] matrix)
    {
        var basis = SolveNullspaceBasis(matrix);
        return basis.Count == 1 ? basis[0] : null;
    }

    /// <summary>
    /// Computes the complete basis of independent integer nullspace reaction vectors for matrix M.
    /// </summary>
    public static IReadOnlyList<int[]> SolveNullspaceBasis(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        // Convert integer matrix to exact rational matrix to prevent floating-point loss
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

        // Compute Reduced Row Echelon Form (RREF)
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

        // Identify all free variable columns (nullspace basis dimension)
        var freeCols = new List<int>();
        for (int c = 0; c < cols; c++)
        {
            if (pivotCols[c] == -1)
            {
                freeCols.Add(c);
            }
        }

        if (freeCols.Count == 0)
        {
            return Array.Empty<int[]>();
        }

        var basisVectors = new List<int[]>();

        foreach (int freeCol in freeCols)
        {
            Rational[] sol = new Rational[cols];
            for (int i = 0; i < cols; i++) sol[i] = new Rational(0, 1);
            sol[freeCol] = new Rational(1);

            for (int c = 0; c < cols; c++)
            {
                if (pivotCols[c] != -1)
                {
                    int r = pivotCols[c];
                    sol[c] = -rMatrix[r, freeCol];
                }
            }

            // Clear denominators via LCM
            BigInteger lcm = 1;
            foreach (var rat in sol)
            {
                if (rat.Num != 0)
                {
                    lcm = LcmBig(lcm, rat.Den);
                }
            }

            var intVec = new int[cols];
            bool allPos = true;
            for (int c = 0; c < cols; c++)
            {
                Rational scaled = sol[c] * new Rational(lcm);
                int val = (int)(scaled.Num / scaled.Den);
                if (val <= 0) allPos = false;
                intVec[c] = val;
            }

            if (!allPos)
            {
                bool flippedAllPos = true;
                for (int c = 0; c < cols; c++)
                {
                    if (-intVec[c] <= 0) flippedAllPos = false;
                }
                if (flippedAllPos)
                {
                    for (int c = 0; c < cols; c++) intVec[c] = -intVec[c];
                }
            }

            // Reduce by GCD
            BigInteger commonGcd = 0;
            for (int c = 0; c < cols; c++)
            {
                if (intVec[c] != 0)
                {
                    commonGcd = commonGcd == 0 ? BigInteger.Abs(intVec[c]) : BigInteger.GreatestCommonDivisor(commonGcd, BigInteger.Abs(intVec[c]));
                }
            }

            if (commonGcd > 1)
            {
                for (int c = 0; c < cols; c++) intVec[c] = (int)(intVec[c] / commonGcd);
            }

            basisVectors.Add(intVec);
        }

        return basisVectors;
    }

    private static BigInteger LcmBig(BigInteger a, BigInteger b)
    {
        if (a == 0 || b == 0) return 0;
        return BigInteger.Abs((a / BigInteger.GreatestCommonDivisor(a, b)) * b);
    }
}

/// <summary>
/// Arbitrary precision rational number structure (Q) for exact algebraic balancing.
/// </summary>
internal readonly struct Rational
{
    public BigInteger Num { get; }
    public BigInteger Den { get; }

    public Rational(BigInteger num, BigInteger den)
    {
        if (den == 0) throw new DivideByZeroException("Rational denominator cannot be zero.");
        if (den < 0) { num = -num; den = -den; }
        BigInteger g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(num), den);
        Num = num / g;
        Den = den / g;
    }

    public Rational(BigInteger num) : this(num, 1) { }

    public static Rational operator +(Rational a, Rational b) =>
        new(a.Num * b.Den + b.Num * a.Den, a.Den * b.Den);

    public static Rational operator -(Rational a, Rational b) =>
        new(a.Num * b.Den - b.Num * a.Den, a.Den * b.Den);

    public static Rational operator -(Rational a) => new(-a.Num, a.Den);

    public static Rational operator *(Rational a, Rational b) =>
        new(a.Num * b.Num, a.Den * b.Den);

    public static Rational operator /(Rational a, Rational b)
    {
        if (b.Num == 0) throw new DivideByZeroException("Cannot divide rational by zero.");
        return new Rational(a.Num * b.Den, a.Den * b.Num);
    }

    public override string ToString() => Den == 1 ? Num.ToString() : $"{Num}/{Den}";
}
