using Chemy.Core.Kinetics;
using Xunit;

namespace Chemy.Core.Tests;

public class KineticsTests
{
    [Fact]
    public void CalculateHalfLife_FirstOrder_CalculatesLn2OverK()
    {
        // t_1/2 = ln(2) / k = 0.693147 / 0.1 ≈ 6.93s
        var result = KineticsEngine.CalculateHalfLife(order: 1, rateConstantK: 0.1);

        Assert.InRange(result.HalfLifeTime, 6.92, 6.94);
    }

    [Fact]
    public void CalculateRateConstant_Arrhenius_CalculatesRateConstantK()
    {
        var result = KineticsEngine.CalculateRateConstant(
            preExponentialFactorA: 1e13,
            activationEnergykJPerMol: 75.0,
            temperatureKelvin: 298.15
        );

        Assert.True(result.RateConstantK > 0);
    }
}
